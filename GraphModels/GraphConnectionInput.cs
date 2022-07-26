using System.Collections.Generic;

namespace ThinkBase.Client.GraphModels
{
    public class GraphConnectionInput
    {
        public List<DarlTimeInput>? existence { get; set; }
        public string lineage { get; set; }
        public string name { get; set; }
        public string startId { get; set; }
        public string endId { get; set; }
        double weight { get; set; } = 1.0;
    }
}

