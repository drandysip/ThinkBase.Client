using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class GraphModel
    {
        public List<StringGraphObjectPair> vertices { get; set; }
        public List<StringGraphConnectionPair> edges { get; set; }
        public List<StringGraphObjectPair> virtualVertices { get; set; }
        public List<StringGraphConnectionPair> virtualEdges { get; set; }
        public List<StringGraphObjectPair> recognitionRoots { get; set; }
        public List<StringGraphObjectPair> recognitionVertices { get; set; }
        public List<StringGraphConnectionPair> recognitionEdges { get; set; }

        public Dictionary<string, GraphObject> Objects { get; set; } = new Dictionary<string, GraphObject>();
        public Dictionary<string, GraphObject> ObjectsByExternalId { get; set; } = new Dictionary<string, GraphObject>();

        public Dictionary<string, GraphConnection> Connections { get; set; } = new Dictionary<string, GraphConnection>();

        public void Init()
        {
            Objects = vertices.ToDictionary(a => a.name, a => a.value);
            ObjectsByExternalId = vertices.ToDictionary(a => a.value.externalId, a => a.value);
            Connections = edges.ToDictionary(a => a.name, a => a.value);
        }
    }
}
