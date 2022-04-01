using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkBase.Client.GraphModels
{
    public class DataMap
    {
        public string relPath { get; set; } = string.Empty;
        public string objId { get; set; } = string.Empty;
        public string attLineage { get; set; } = "answer";
        public bool target { get; set; } = false;
        public string objectLineage { get; set; } = string.Empty;
        public string? objectSubLineage { get; set; }
        public GraphAttribute.DataType dataType { get; set; } = GraphAttribute.DataType.CATEGORICAL;
    }
}
