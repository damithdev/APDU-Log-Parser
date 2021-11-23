using BerTlv;
using Microsoft.Win32;
using Pax_APDU_Log_Parser.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        BackgroundWorker worker;
        private void Window_ContentRendered(object sender, EventArgs e)
        {
             worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();
            setStatus("Ready !", 0);
            worker.CancelAsync();

        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                (sender as BackgroundWorker).ReportProgress(progress);
                Thread.Sleep(100);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
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
                worker.RunWorkerAsync();

                setStatus("Extracting");
                tbMultiLine.Text = "";
                var item = files[0];
                string fileName = System.IO.Path.GetFileName(item);
                string extension = System.IO.Path.GetExtension(item);
                if (acceptedExtensions.Contains(extension))
                {
                    var rootText = ReadFromFileStream(item);
                    tbMultiLine.Text = rootText;
                    setStatus("Validated", 100);
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Invalid File Extension " + extension);
                    _files.Clear();
                    setStatus("Invalid File Extension",0);

                }



            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("An error occurred!");
                setStatus("Error",0);

            }
            worker.CancelAsync();

        }

        private String ReadFromFileStream(String file)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8);

            string line = String.Empty;
            string logLines = String.Empty;
            //string previousLineTlv = String.Empty;
            setStatus("Reading");

            while ((line = sr.ReadLine()) != null)
            {

                if (line.Contains("apdu") && line.Contains("W/"))
                {
                    logLines += line + "\n";
                    setStatus("Reading",progress + 1);

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
                setStatus("Error", 0);

            }
            worker.CancelAsync();

        }

        private void parseAndShowLog(string log)
        {
            worker.RunWorkerAsync();

            setStatus("Parsing", 0);

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
                int counter = 1;
                foreach (var item in lines)
                {
                    setStatus("Parsing",progress + (90 - progress)/lines.Length);
                    if (item.Contains(cmdLine))
                    {
                        string body = rgx.Replace(item.Substring(item.IndexOf(cmdLine) + cmdLine.Length),"");
                        printCommandType(body,ref text,counter);
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
                        text += ". ============================================================== \n\n";
                    }
                }
                setStatus("Parsed", 100);

            }
            else
            {
                MessageBox.Show("Unknown Log Type");
                setStatus("Parse Error", 0);


            }



            tbMultiLineParsed.Text = text;
            worker.CancelAsync();

        }

        private void printCommandType(String command,ref string text,int counter)
        {
            string cla = command.Substring(0, 2);
            string ins = command.Substring(2, 2);

            string msg = "UNKNOWN MESSEAGE";
            string msg2 = "";

            if(cla == "8C" || cla == "84")
            {
                if(ins == "1E")
                {
                    //  The APPLICATION BLOCK command

                    //  The APPLICATION BLOCK command is a post - issuance command that
                    //  invalidates the currently selected application.
                    //  Following the successful completion of an APPLICATION BLOCK command:
                    //          • An invalidated application shall return the status bytes SW1 SW2 = '6283'
                    //          (‘Selected file invalidated’) in response to a SELECT command.
                    //          • An invalidated application shall return only an Application Authentication
                    //          Cryptogram(AAC) as AC in response to a GENERATE AC command.

                    // EMV Book 3 Section 6.5.1

                    msg = "APPLICATION BLOCK";
                }else if(ins == "18")
                {
                    //  The APPLICATION UNBLOCK command

                    //  The APPLICATION UNBLOCK command is a post - issuance command that
                    //  rehabilitates the currently selected application.
                    //  Following the successful completion of an APPLICATION UNBLOCK command,
                    //  the restrictions imposed by the APPLICATION BLOCK command are removed.

                    // EMV Book 3 Section 6.5.2

                    msg = "APPLICATION UNBLOCK";
                }else if (ins == "16")
                {
                    //  The CARD BLOCK command

                    //  The CARD BLOCK command is a post - issuance command that permanently
                    //  disables all applications in the ICC.
                    //  The CARD BLOCK command shall disable all applications in the ICC, including
                    //  applications that may be selected implicitly.
                    //  Following the successful completion of a CARD BLOCK command, all subsequent
                    //  SELECT commands shall return the status bytes SW1 SW2 = '6A81'(‘Function
                    //  not supported’) and perform no other action.

                    // EMV Book 3 Section 6.5.3

                    msg = "CARD BLOCK";
                }else if(ins == "24")
                {
                    //  The PIN CHANGE/UNBLOCK command

                    //  The PIN CHANGE / UNBLOCK command is a post - issuance command.Its
                    //  purpose is to provide the issuer the capability either to unblock the PIN or to
                    //  simultaneously change and unblock the reference PIN.
                    //  Upon successful completion of the PIN CHANGE / UNBLOCK command, the card
                    //  shall perform the following functions:
                    //      • The value of the PIN Try Counter shall be reset to the value of the PIN Try Limit.
                    //      • If requested, the value of the reference PIN shall be set to the new PIN value.
                    //  If PIN data is transmitted in the command it shall be enciphered for confidentiality.

                    // EMV Book 3 Section 6.5.10

                    msg = "PIN CHANGE/UNBLOCK";


                }

            }
            else if(cla == "00")
            {
                if(ins == "A4")
                {
                    msg = "SELECT COMMAND";
                }
                else if(ins == "82")
                {
                    //  The EXTERNAL AUTHENTICATE command 

                    //  The EXTERNAL AUTHENTICATE command asks the application in the ICC to
                    //  verify a cryptogram.
                    //  The ICC returns the processing state of the command.

                    // EMV Book 3 Section 6.5.4

                    msg = "EXTERNAL AUTHENTICATE";
                }else if(ins == "84")
                {
                    //  The GET CHALLENGE command

                    //  The GET CHALLENGE command is used to obtain an unpredictable number
                    //  from the ICC for use in a security - related procedure.
                    //  The challenge shall be valid only for the next issued command.

                    // EMV Book 3 Section 6.5.6

                    msg = "GET CHALLENGE";

                }else if(ins == "88")
                {

                    // The INTERNAL AUTHENTICATE command

                    //  The INTERNAL AUTHENTICATE command initiates the computation of the
                    //  Signed Dynamic Application Data by the card using:
                    //      • the challenge data sent from the terminal and
                    //      • ICC data and
                    //      • a relevant private key stored in the card.
                    //  The ICC returns the Signed Dynamic Application Data to the terminal.

                    // EMV Book 3 Section 6.5.9
                    msg = "INTERNAL AUTHENTICATE";

                }else if(ins == "B2")
                {
                    // The READ RECORD command

                    // The READ RECORD command reads a file record in a linear file.
                    // The response from the ICC consists of returning the record.

                    msg = "READ RECORD";

                }else if(ins == "20")
                {
                    //  The VERIFY command

                    //  The VERIFY command initiates in the ICC the comparison of the Transaction
                    //  PIN Data sent in the data field of the command with the reference PIN data
                    //  associated with the application.The manner in which the comparison is
                    //  performed is proprietary to the application in the ICC.
                    //  The VERIFY command applies when the Cardholder Verification Method(CVM)
                    //  chosen from the CVM List is an offline PIN.

                    msg = "VERIFY";

                }
            }
            else if(cla == "80")
            {
                if(ins == "AE")
                {
                    //  The GENERATE AC command

                    //  The GENERATE AC command sends transaction-related data to the ICC, which
                    //  computes and returns a cryptogram.This cryptogram shall either be an
                    //  Application Cryptogram(AC) as specified in this specification or a proprietary
                    //  cryptogram.In both cases, the cryptogram shall be of a type specified


                    //  Application Authentication Cryptogram (AAC) - Transaction declined
                    //  Authorisation Request Cryptogram (ARQC) - Online authorisation requested
                    //  Transaction Certificate (TC) - Transaction approved

                    // EMV Book 3 Section 6.5.5


                    msg = "GENERATE APPLICATION CRYPTOGRAM";

                    var ctrl = command.Substring(4, 2);

                    switch (ctrl)
                    {
                        case "00":
                            msg2 = "AAC";
                            break;
                        case "10":
                            msg2 = "AAC + CDA";
                            break;
                        case "40":
                            msg2 = "TC";
                            break;
                        case "50":
                            msg2 = "TC + CDA";
                            break;
                        case "80":
                            msg2 = "ARQC";
                            break;
                        case "90":
                            msg2 = "ARQC + CDA";
                            break;
                        default:
                            msg2 = "UNKNOWN GAC Parameter";
                            break;

                    }

                    
                }else if(ins == "CA")
                {
                    //  The GET DATA command

                    //  The GET DATA command is used to retrieve a primitive data object not
                    //  encapsulated in a record within the current application.
                    //  The usage of the GET DATA command in this specification is limited to the
                    //  retrieval of the following primitive data objects that are defined in Annex A and
                    //  interpreted by the application in the ICC:
                    //      • ATC(tag '9F36')
                    //      • Last Online ATC Register(tag '9F13')
                    //      • PIN Try Counter(tag '9F17')
                    //      • Log Format(tag '9F4F')

                    // EMV Book 3 Section 6.5.7

                    msg = "GET DATA";

                }else if(ins == "A8")
                {
                    //  The GET PROCESSING OPTIONS command

                    //  The GET PROCESSING OPTIONS command initiates the transaction within the ICC.
                    //  The ICC returns the Application Interchange Profile(AIP) and the Application File Locator(AFL).

                    // EMV Book 3 Section 6.5.8

                    msg = "GET PROCESSING OPTIONS";

                }
            }

            text += "\n" +counter+") "+ msg + "\n";
            if (msg2 != "") text += "\t"+ msg2 + "\n";
            text += "_____________________________\n";
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

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            _files.Clear();
            tbMultiLine.Clear();
            tbMultiLineParsed.Clear();
            setStatus("Ready !",0);

        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "Parsed Log " + DateTime.Now.ToFileTime();
            saveFileDialog.DefaultExt = ".pLog";
            if (saveFileDialog.ShowDialog() == true)
            {
                worker.RunWorkerAsync();

                File.WriteAllText(saveFileDialog.FileName, tbMultiLineParsed.Text);
                setStatus("Saved", 100);
            }

            worker.CancelAsync();

        }

        private void tbMultiLine_TextChanged(object sender, TextChangedEventArgs e)
        {
            buttonParse.IsEnabled = tbMultiLine.Text != string.Empty;
            buttonClearEnabledStateUpdate();

        }

        private void tbMultiLineParsed_TextChanged(object sender, TextChangedEventArgs e)
        {
            buttonSave.IsEnabled = tbMultiLineParsed.Text != string.Empty;
            buttonClearEnabledStateUpdate();
        }

        private void buttonClearEnabledStateUpdate()
        {
            if (buttonParse.IsEnabled || buttonSave.IsEnabled)
            {
                buttonClear.IsEnabled = true;
            }
            else
            {
                buttonClear.IsEnabled = false;
            }
        }

        BackgroundWorker bgWorkerExport;

        private static int progress = 0;
        private void setStatus(String text,int percentage = -1)
        {
            StatusIndicator.Text = text;
            if (percentage == -1)
            {
                if(progress < 90)progress += 5;
            }
            else
            {
                progress = percentage;
            }
        }
    }



}
