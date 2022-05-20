using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkBase.Client.GraphModels
{
    public class GraphObjectInput
    {
        public List<DarlTimeInput>? existence { get; set; }
        public string lineage { get; set; } = "noun:00,0";
        public string? subLineage { get; set; }
        public string name { get; set; } = "";
        public string externalId { get; set; } = "";
        public List<GraphAttributeInput>? properties {get; set;}
    }
}

