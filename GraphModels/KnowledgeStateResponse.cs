﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KnowledgeStateResponse
    {
        public KnowledgeStateInput? getKnowledgeState { get; set; }
        public KnowledgeStateInput? getInteractKnowledgeState { get; set; }

        public KnowledgeStateInput? createKnowledgeState { get; set; }

        public List<KnowledgeStateInput> createKnowledgeStateList { get; set; } = new List<KnowledgeStateInput>();
        public List<KnowledgeStateInput> getKnowledgeStatesByType { get; set; } = new List<KnowledgeStateInput>();
        public List<KnowledgeStateInput> getKnowledgeStatesByTypeAndAttribute { get; set; } = new List<KnowledgeStateInput>();


    }
}
