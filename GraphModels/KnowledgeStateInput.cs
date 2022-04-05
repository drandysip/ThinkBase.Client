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

        public KnowledgeState Convert()
        {
            var ks = new KnowledgeState { created = DateTime.UtcNow, subjectId = subjectId, knowledgeGraphName = knowledgeGraphName, data = new Dictionary<string, List<GraphAttribute>>() };
            foreach(var pair in data)
            {
                ks.data.Add(pair.name, new List<GraphAttribute>());
                foreach(var a in pair.value)
                {
                    ks.data[pair.name].Add(new GraphAttribute { value = a.value, confidence = a.confidence ?? 0.0, existence = a.existence, lineage = a.lineage, name = a.name, type = a.type });
                }
            }
            return ks;
        }

    }
}
