using System;
using System.Collections.Generic;
using System.Text;

namespace APDU_Log_Parser.Models
{
    public class TlvValue
    {
        public string tag { get; set; }

        public string hexLength { get; set; }

        public int length { get; set; }

        public string value { get; set; }

        public List<TlvValue> parsedValue { get; set; }

    }
}
