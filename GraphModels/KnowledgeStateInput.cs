using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeStateInput
    {
        public string subjectId { get; set; }
        public string knowledgeGraphName { get; set; }

        public bool transient { get; set; } = false;
        public List<StringListGraphAttributeInputPair> data { get; set; } = new List<StringListGraphAttributeInputPair>();

    }
}
