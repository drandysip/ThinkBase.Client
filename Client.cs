using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkBase.Client.GraphModels;

namespace ThinkBase.Client
{
    public class Client : IClient
    {
        private string _graphName;
        private GraphQLHttpClient client;
        private GraphModel _model;

        public Client(string authcode, string graphName)
        {
            client = new GraphQLHttpClient("https://darl.dev/graphql/", new SystemTextJsonSerializer());
            if(!string.IsNullOrEmpty(authcode))
                client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authcode);
            _graphName = graphName;
        }

        public async Task<GraphModel> FetchModel()
        {
            var modelReq = new GraphQLHttpRequest
            {
                Variables = new { name = _graphName },
                Query = "query ($name: String!){kGraphByName(name: $name){name model{vertices{name value{lineage subLineage id externalId properties{lineage name value }}} edges {name value{lineage endId startId name inferred weight id}}}}}"
            };
            var model = await client.SendQueryAsync<KGraphResponse>(modelReq);
            if (model.Errors != null && model.Errors.Count() > 0)
                throw new Exception(model.Errors[0].Message);
            _model = model.Data.kGraphByName.model;
            _model.Init();
            return _model;
        }

        public async Task<KnowledgeState> GetKnowledgeState(string subjectId)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName, id = subjectId },
                Query = @"query ($name: String! $id: String!){getKnowledgeState(graphName: $name id: $id){knowledgeGraphName subjectId data{ name value {name type value lineage inferred confidence}}}}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            return resp.Data.getKnowledgeState;
        }


        /// <summary>
        /// Add a new Knowledge State to the Graph
        /// </summary>
        /// <param name="ks">The Knowledge State</param>
        /// <returns>The Knowledge State Added</returns>
        public async Task<KnowledgeState> CreateKnowledgeState(KnowledgeState ks)
        {
            var ksi = new KnowledgeStateInput { knowledgeGraphName = ks.knowledgeGraphName, subjectId = ks.subjectId };
            foreach(var c in ks.data.Keys)
            {
                var l = new List<GraphAttributeInput>();
                foreach(var p in ks.data[c])
                {
                    l.Add(new GraphAttributeInput { confidence = p.confidence, existence = p.existence, inferred = p.inferred, lineage = p.lineage, name = p.name, type = p.type, value = p.value });
                }
                ksi.data.Add(new StringListGraphAttributeInputPair { Name = c, Value =  l});
            }
            var req = new GraphQLHttpRequest()
            {
                Variables = new { ks = ksi },
                Query = @"mutation ($ks: knowledgeStateInput!){ createKnowledgeState(ks: $ks ){subjectId }}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            return resp.Data.createKnowledgeState;
        }

        /// <summary>
        /// Removes all knowledge states for this graph
        /// </summary>
        /// <returns>The count of Knowledge States removed</returns>
        public async Task<long> ClearAllKnowledgeStates()
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName },
                Query = @"mutation ($name: String!){ deleteAllKnowledgeStates(name: $name)}"
            };
            var resp = await client.SendQueryAsync<DeleteAllKnowledgeStatesResponse>(req);
            return resp.Data.deleteAllKnowledgeStates;
        }

        public async Task SetDataValue(KnowledgeState ks, string nodeName, string attName, string value)
        {
            if (_model == null)
                await FetchModel();
            if (!_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            ks.knowledgeGraphName = _graphName;
            var obj = _model.ObjectsByExternalId[nodeName];
            bool found = false;
            foreach (var l in obj.properties)
            {
                if (l.name == attName)
                {
                    if(!ks.data.ContainsKey(obj.id))
                    {
                        ks.data.Add(obj.id, new List<GraphAttribute>());
                    }
                    var att = ks.data[obj.id].FirstOrDefault(a => a.name == attName);
                    if (att == null)
                    {
                        ks.data[obj.id].Add(new GraphAttribute { value = value, type = l.type, lineage = l.lineage, confidence = l.confidence, id = Guid.NewGuid().ToString(), name = l.name, inferred = true });
                    }
                    else
                    {
                        att.value = value;
                    }
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                throw new ThinkBaseException($"Attribute {attName} not found. Schema change?");
            }
        }

        public async Task SetConnectionPresence(KnowledgeState ks, string nodeName, string destName, string remoteSubjectId)
        {
            if (_model == null)
                await FetchModel();
            if (!_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            if (!_model.ObjectsByExternalId.ContainsKey(destName))
                throw new ArgumentOutOfRangeException($"{destName} not found in {_graphName}");
            var remoteId = _model.ObjectsByExternalId[destName].id;
            ks.knowledgeGraphName = _graphName;
            var obj = _model.ObjectsByExternalId[nodeName];
            var link = _model.Connections.Values.FirstOrDefault(a => a.startId == obj.id && a.inferred && a.endId == remoteId);
            if(link != null)
            {
                if (!ks.data.ContainsKey(link.id))
                {
                    ks.data.Add(link.id, new List<GraphAttribute>());
                }
                ks.data[link.id].Add(new GraphAttribute { name = link.name, type = GraphAttribute.DataType.connection, confidence = link.weight, lineage = link.lineage, id = link.id, inferred = true, value = remoteSubjectId });
            }
            else
            {
                throw new ThinkBaseException($"Connection from {nodeName} to {destName} not found. Schema change?");
            }
        }
    }
}
