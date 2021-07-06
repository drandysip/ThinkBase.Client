using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class GraphElement
    {
        public string id { get; set; }
            public string name { get; set; }
            public string lineage { get; set; }
 
       public List<DarlTime?> existence { get; set; }
            public bool inferred { get; set; }
            public bool? _virtual { get; set; }
       public List<GraphAttribute> properties { get; set; }
            public string dynamicSource { get; set; }


    }
}
