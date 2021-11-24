using BerTlv;
using LumenWorks.Framework.IO.Csv;
using APDU_Log_Parser.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace APDU_Log_Parser
{
    public class TLVParserUtility
    {
        public static Dictionary<string,EmvTag> emvTags = new Dictionary<string, EmvTag>();
        public static List<TlvValue> getParsedTLV(string rawTLV)
        {
            var cleanedTLV = replaceWhitespace(rawTLV, "");
            if (!OnlyHexInString(cleanedTLV))
            {
                throw new FormatException("Invalid Hex");
            }

            var rootMap = GetTlvValue(cleanedTLV);
            return rootMap;
        }

        private static List<TlvValue> GetTlvValue(String value)
        {

            try
            {
                ICollection<Tlv> tlvs = Tlv.ParseTlv(value);
                Console.WriteLine(tlvs);

                var tlvMap = new List<TlvValue>();

                foreach (Tlv item in tlvs)
                {
                    tlvMap.Add(new TlvValue { tag = item.HexTag, hexLength = item.HexLength, length = item.Length, value = item.HexValue, parsedValue = GetTlvValue(item.HexValue) });
                }
                return tlvMap;
            }
            catch (Exception)
            {
                return null;
            }


        }

        private static readonly Regex sWhitespace = new Regex(@"\s+");
        public static string replaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }

        public static bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        public static bool isTlvValue(String value)
        {

            try
            {
                ICollection<Tlv> tlvs = Tlv.ParseTlv(value);
                Console.WriteLine(tlvs);

                return true;
            }
            catch (Exception)
            {
                return false;
            }


        }

        public static void InitEmvTags()
        {
            var csvText = APDU_Log_Parser.Properties.Resources.emvtags;
            var csvTable = new DataTable();

            using (var csvReader = new CsvReader(new StringReader(csvText), true))
            {
                csvTable.Load(csvReader);
            }

            for (int i = 0; i < csvTable.Rows.Count; i++)
            {
                try
                {
                    emvTags.Add(csvTable.Rows[i][0].ToString(), new EmvTag
                    {
                        tag = csvTable.Rows[i][0].ToString(),
                        name = csvTable.Rows[i][1].ToString(),
                        description = csvTable.Rows[i][2].ToString(),
                        source = csvTable.Rows[i][3].ToString(),
                        format = csvTable.Rows[i][4].ToString(),
                        template = csvTable.Rows[i][5].ToString(),
                        minLength = csvTable.Rows[i][6].ToString(),
                        maxLength = csvTable.Rows[i][7].ToString(),
                        pc = csvTable.Rows[i][8].ToString(),
                        example = csvTable.Rows[i][9].ToString(),
                    });
                }
                catch (Exception)
                {

                }
                
            }
            
        }
    }
}
