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
            numeric = 0,
            categorical = 1,
            textual = 2,
            sequence = 3,
            temporal = 4,
            duration = 5,
            markdown = 6,
            ruleset = 7,
            link = 8,
            connection = 9
        }
    }
}
