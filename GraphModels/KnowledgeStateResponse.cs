using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeStateResponse
    {
        public KnowledgeStateInput getKnowledgeState { get; set; }
        public KnowledgeStateInput createKnowledgeState { get; set; }

        public List<KnowledgeStateInput> createKnowledgeStateList { get; set; }
        public List<KnowledgeStateInput> getKnowledgeStatesByType { get; set; }

    }
}
