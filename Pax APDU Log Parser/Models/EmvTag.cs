using System;
using System.Collections.Generic;
using System.Text;

namespace Pax_APDU_Log_Parser.Models
{
    public class EmvTag
    {
        public string tag { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string source { get; set; }
        public string format { get; set; }
        public string template { get; set; }
        public string minLength { get; set; }
        public string maxLength { get; set; }
        public string pc { get; set; }
        public string example { get; set; }

    }
}
