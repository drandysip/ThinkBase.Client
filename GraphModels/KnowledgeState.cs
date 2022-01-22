using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeState
    {
        public string subjectId { get; set; } = string.Empty;
        public string knowledgeGraphName { get; set; } = string.Empty;
        public Dictionary<string, List<GraphAttribute>> data { get; set; } = new Dictionary<string, List<GraphAttribute>>();
        public DateTime? created { get; set; }

        public GraphAttribute? GetAttribute(string id, string lineage)
        {
            if (data.ContainsKey(id))
                return data[id].FirstOrDefault(a => a.lineage == lineage);
            return null;
        }
    }
}
