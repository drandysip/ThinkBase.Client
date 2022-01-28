using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class GraphObject : GraphElement
    {
        public string externalId { get; set; } = string.Empty;
        public List<GraphConnection>? Out { get; set; }
        public List<GraphConnection>? In { get; set; }

    }
}
