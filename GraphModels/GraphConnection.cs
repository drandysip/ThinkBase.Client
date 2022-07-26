using System;

namespace ThinkBase.Client.GraphModels
{
    public class GraphConnection : GraphElement
    {
        public double weight { get; set; }
        public string startId { get; set; } = String.Empty;
        public string endId { get; set; } = String.Empty;
    }
}
