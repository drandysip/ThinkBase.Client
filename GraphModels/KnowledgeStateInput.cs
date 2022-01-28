using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeStateInput
    {
        public string subjectId { get; set; } = string.Empty;
        public string knowledgeGraphName { get; set; } = string.Empty;

        public bool transient { get; set; } = false;
        public List<StringListGraphAttributeInputPair> data { get; set; } = new List<StringListGraphAttributeInputPair>();

    }
}
