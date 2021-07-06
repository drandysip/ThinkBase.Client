using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeStateInput
    {
        public string subjectId { get; set; }
        public string knowledgeGraphName { get; set; }
        public List<StringListGraphAttributePair> data { get; set; } = new List<StringListGraphAttributePair>();

    }
}
