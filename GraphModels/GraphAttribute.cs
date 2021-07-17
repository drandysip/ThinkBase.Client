using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class GraphAttribute : GraphElement
    {

        public string value { get; set; }
        public double confidence { get; set; }
        public DataType type { get; set; }


        public enum DataType
        {
            NUMERIC = 0,
            CATEGORICAL = 1,
            TEXTUAL = 2,
            SEQUENCE = 3,
            TEMPORAL = 4,
            DURATION = 5,
            MARKDOWN = 6,
            RULESET = 7,
            LINK = 8,
            CONNECTION = 9
        }
    }
}
