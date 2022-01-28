using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class GraphElement
    {
        public string id { get; set; } = String.Empty;
        public string name { get; set; } = String.Empty;
        public string lineage { get; set; } = String.Empty;

        public List<DarlTime>? existence { get; set; }
        public bool inferred { get; set; }
        public bool? _virtual { get; set; }
        public List<GraphAttribute>? properties { get; set; }
        public string dynamicSource { get; set; } = String.Empty;


    }
}
