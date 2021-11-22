using BerTlv;
using Microsoft.Win32;
using Pax_APDU_Log_Parser.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pax_APDU_Log_Parser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly string[] acceptedExtensions = new string[] { ".log", ".txt" };
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            TLVParserUtility.InitEmvTags();

        }

        public ObservableCollection<string> Files
        {
            get
            {
                return _files;
            }
        }
        private ObservableCollection<string> _files = new ObservableCollection<string>();

        private void DropBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                var listbox = sender as ListBox;
                listbox.Background = new SolidColorBrush(Color.FromRgb(155, 155, 155));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DropBox_DragLeave(object sender, DragEventArgs e)
        {
            var listbox = sender as ListBox;
            listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
        }

        private void DropBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                _files.Clear();
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                _files.Add(files[0]);

                ValidateFiles(files);
            }

            var listbox = sender as ListBox;
            listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
        }

        private void ValidateFiles(string[] files)
        {
            try
            {
                tbMultiLine.Text = "";
                var item = files[0];
                string fileName = System.IO.Path.GetFileName(item);
                string extension = System.IO.Path.GetExtension(item);
                if (acceptedExtensions.Contains(extension))
                {
                    var rootText = ReadFromFileStream(item);
                    tbMultiLine.Text = rootText;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Invalid File Extension " + extension);
                    _files.Clear();
                }


            }catch(Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("An error occurred!");
            }
        }

        private String ReadFromFileStream(String file)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8);

            string line = String.Empty;
            string logLines = String.Empty;
            //string previousLineTlv = String.Empty;

            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("apdu") && line.Contains("W/"))
                {
                    logLines += line + "\n";

                }
              
            }

            return logLines;
        }

        public IEnumerable<string> ReadLines(Func<Stream> streamProvider,
                                     Encoding encoding)
        {
            using (var stream = streamProvider())
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }




        private void buttonParse_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                    String log = tbMultiLine.Text;
                    if (log != null && log != "")
                    {
                        parseAndShowLog(log);
                    }

            }
            catch (Exception)
            {
                MessageBox.Show("Error Occurred");
            }
  
        }

        private void parseAndShowLog(string log)
        {
            var cmdLine = "apduSend = [Command]";
            var lcLine = "apduSend = [LC]";
            var dataLine = "apduSend = [Data]";
            var leLine = "apduSend = [LE]";
            var rcvCmd = "apduRecvCmd = ";
            var rcvState = "apduRecvState = ";
            var seperator = "===========";

            String text = String.Empty;

            if (log.Contains(cmdLine))
            {
                string[] lines = log.Split("\n");
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                int counter = 0;
                foreach (var item in lines)
                {
                    if (item.Contains(cmdLine))
                    {
                        string body = rgx.Replace(item.Substring(item.IndexOf(cmdLine) + cmdLine.Length),"");
                        text += "[Command] "+body+"\n";

                    }
                    else if (item.Contains(lcLine))
                    {
                        string body = rgx.Replace(item.Substring(item.IndexOf(lcLine) + lcLine.Length),"");
                        text += "[LC] " + body + "\n";
                    }
                    else if (item.Contains(dataLine))
                    {
                        string body =  rgx.Replace(item.Substring(item.IndexOf(dataLine) + dataLine.Length), "");

                        text += "[Data] " + body + "\n";

                        if (TLVParserUtility.OnlyHexInString(body) && TLVParserUtility.isTlvValue(body))
                        {
                            List<TlvValue> parsedTlv = TLVParserUtility.getParsedTLV(body);
                            text += "\n";
                            printText(ref text, parsedTlv, 0);
                        }

                    }
                    else if (item.Contains(leLine))
                    {
                        string body = rgx.Replace(item.Substring(item.IndexOf(leLine) + leLine.Length),"");
                        text += "[LE] " + body + "\n";
                    }
                    else if (item.Contains(rcvCmd))
                    {
                        string body = rgx.Replace(item.Substring(item.IndexOf(rcvCmd) + rcvCmd.Length), "");
                        text += "[Response] " + body + "\n";

                        if (TLVParserUtility.OnlyHexInString(body) && TLVParserUtility.isTlvValue(body))
                        {
                            List<TlvValue> parsedTlv = TLVParserUtility.getParsedTLV(body);
                            text += "\n";
                            printText(ref text, parsedTlv, 0);
                        }
                    }
                    else if (item.Contains(rcvState))
                    {
                        string body = rgx.Replace(item.Substring(item.IndexOf(rcvState) + rcvState.Length),"");
                        text += "[State] " + body + "\n";
                    }
                    else if (item.Contains(seperator))
                    {
                        counter++;
                        text += counter +". ============================================================== \n\n";
                    }
                }
            }
            else
            {
                MessageBox.Show("Unknown Log Type");
            }



            tbMultiLineParsed.Text = text;
            
        }

        private void parseAndShowLog(List<Command> list)
        {
            var rootText = string.Empty;
            foreach (var command in list)
            {
                rootText += command.send ? "T" : "C";
                rootText += "- " + command.command + "\n";

                List<TlvValue> parsedTlv = null;
                try
                {
                    parsedTlv = TLVParserUtility.getParsedTLV(command.command);
                }
                catch (Exception)
                {

                }
                if(parsedTlv != null)
                {
                    printText(ref rootText, parsedTlv, 0);
                }
                rootText += "\n";

            }

            tbMultiLineParsed.Text = rootText;

        }

        private void printText(ref string text, List<TlvValue> tlv,int level)
        {
            if(tlv != null)
            {
                foreach (var item in tlv)
                {
                    if (TLVParserUtility.emvTags.ContainsKey(item.tag))
                    {
                        EmvTag tag = TLVParserUtility.emvTags.GetValueOrDefault(item.tag);

                        text += getTabSpace(level) + item.tag + " " + item.hexLength + "(" + item.length + ") " + item.value + "\n";
                        text += getTabSpace(level) + tag.name + "\n";
                        text += "\n";
                        printText(ref text, item.parsedValue, level + 1);
                    }
                    //text += getTabSpace(level)+item.value + "\n";


                }
            }

        }

        private string getTabSpace(int tabs)
        {
            var tabSpace = string.Empty;
            for (int i = 0; i < tabs; i++)
            {
                tabSpace += "\t";
            }

            return tabSpace;
        }

      

        private void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Text (*.txt,*.log)|*.txt;*.log";
            dialog.Multiselect = true;
            dialog.Title = "Open Log Files";

            if (dialog.ShowDialog() == true)
            {
                _files.Clear();
                // Read the files
                foreach (var file in dialog.FileNames)
                {
                    _files.Add(file);
                }

                ValidateFiles(dialog.FileNames);
            }
        }
    }



}
