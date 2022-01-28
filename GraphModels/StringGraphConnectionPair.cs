using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class StringGraphConnectionPair
    {
        public string name { get; set; } = string.Empty;
        public GraphConnection? value { get; set; }
    }
}
