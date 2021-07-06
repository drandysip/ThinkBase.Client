using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeState
    {
        public string subjectId { get; set; }
        public string knowledgeGraphName { get; set; }
         public Dictionary<string, List<GraphAttribute>> data { get; set; }
    }
}
