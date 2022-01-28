using System;
using System.Collections.Generic;
using System.Text;
using static ThinkBase.Client.GraphModels.GraphAttribute;

namespace ThinkBase.Client.GraphModels
{
    public class GraphAttributeInput 
    {
        public string value { get; set; } = String.Empty;
        public double? confidence { get; set; } 
        public DataType type { get; set; }
        public string name { get; set; } = String.Empty;
        public string lineage { get; set; } = String.Empty;
        public string subLineage { get; set; } = String.Empty;
        public List<DarlTime>? existence { get; set; }//existence
        public bool? inferred { get; set; }

    }
}
