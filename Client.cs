using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
        private int batchLength { get; set; } = 10;

        ITraceWriter traceWriter = new MemoryTraceWriter();

        public Client(string authcode, string graphName, string path = "https://darl.dev/graphql/")
        {
            client = new GraphQLHttpClient(path, new NewtonsoftJsonSerializer(new JsonSerializerSettings
            {
                TraceWriter = traceWriter,
                ContractResolver = new CamelCasePropertyNamesContractResolver { IgnoreIsSpecifiedMembers = true },
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters = { new ConstantCaseEnumConverter() }
            }));
            if(!string.IsNullOrEmpty(authcode))
                client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authcode);
            _graphName = graphName;
        }


        /// <summary>
        /// Get a read-only version of the graph
        /// </summary>
        /// <returns></returns>
        public async Task<GraphModel> FetchModel()
        {
            var modelReq = new GraphQLHttpRequest
            {
                Variables = new { name = _graphName },
                Query = "query ($name: String!){kGraphByName(name: $name){name model{vertices{name value{lineage subLineage id externalId properties{lineage name value type existence {raw precision } } existence {raw precision }}} edges {name value{lineage endId startId name inferred weight id existence {raw precision }}}}}}"
            };
            var model = await client.SendQueryAsync<KGraphResponse>(modelReq);
            if (model.Errors != null && model.Errors.Count() > 0)
                throw new Exception(model.Errors[0].Message);
            if(model.Data.kGraphByName == null)
                throw new Exception($"{_graphName} is not present in this account.");
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
            var ksi =  resp.Data.getKnowledgeState;
            if(ksi != null)
                return new KnowledgeState { knowledgeGraphName = _graphName, subjectId = subjectId, data = ksi.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) };
            return null;
        }

        public async Task<List<KnowledgeState>> GetChildKnowledgeStates(string parentId)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName, typeObjectId = parentId },
                Query = @"query ($name: String! $id: String!){getKnowledgeStatesByType(graphName: $name typeObjectId: $typeObjectId){knowledgeGraphName subjectId data{ name value {name type value lineage inferred confidence}}}}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            var ksi = resp.Data.getKnowledgeStatesByType;
            var list = new List<KnowledgeState>();
            if (ksi != null)
            {
                foreach(var k in ksi)
                {
                    list.Add(new KnowledgeState { knowledgeGraphName = _graphName, subjectId = k.subjectId, data = k.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) })
                }
            }
            return list;
        }
    


        /// <summary>
        /// Add a new Knowledge State to the Graph or overwrite existing
        /// </summary>
        /// <remarks>Deletes any existing KS with the same userId, Graph and subjectId.</remarks>
        /// <param name="ks">The Knowledge State</param>
        /// <param name="asSystem">Perform as System - Admin level API keys only</param>
        /// <returns>The Knowledge State Added</returns>
        public async Task<KnowledgeState> CreateKnowledgeState(KnowledgeState ks, bool? asSystem)
        {
            var ksi = ConvertKnowledgeState(ks);
            GraphQLHttpRequest req;
            if (asSystem ?? false)
            {
                req = new GraphQLHttpRequest()
                {
                    Variables = new { ks = ksi },
                    Query = @"mutation ($ks: knowledgeStateInput!){ createKnowledgeState(ks: $ks asSystem: true ){knowledgeGraphName subjectId data{ name value {name type value lineage inferred confidence}}}}"
                };

            }
            else
            {
                req = new GraphQLHttpRequest()
                {
                    Variables = new { ks = ksi },
                    Query = @"mutation ($ks: knowledgeStateInput!){ createKnowledgeState(ks: $ks ){knowledgeGraphName subjectId data{ name value {name type value lineage inferred confidence}}}}"
                };
            }
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            var ksinew = resp.Data.createKnowledgeState;
            return new KnowledgeState { knowledgeGraphName = _graphName, subjectId = ksinew.subjectId, data = ksinew.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) };
        }

        /// <summary>
        /// Add a list of Knowledge States to the graph or overwrite existing, using batching to increase performance.
        /// </summary>
        /// <param name="ks"></param>
        /// <returns></returns>
        public async Task<List<KnowledgeState>> CreateKnowledgeStateBatched(List<KnowledgeState> ksl)
        {
            bool complete = false;
            int index = 0;
            var ksis = new List<KnowledgeStateInput>();
            var results = new List<KnowledgeState>();
            while(!complete)
            {
                for(int n = 0; n < batchLength && n + index < ksl.Count; n++)
                {
                    ksis.Add(ConvertKnowledgeState(ksl[n + index]));
                }
                var req = new GraphQLHttpRequest()
                {
                    Variables = new { ks = ksis },
                    Query = @"mutation ($ks: [knowledgeStateInput]!){ createKnowledgeStateList(ksl: $ks ){knowledgeGraphName subjectId data{ name value {name type value lineage inferred confidence}}}}"
                };
                var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
                if (resp.Errors != null && resp.Errors.Count() > 0)
                    throw new Exception(resp.Errors[0].Message);
                foreach(var r in resp.Data.createKnowledgeStateList)
                {
                    results.Add(new KnowledgeState { knowledgeGraphName = _graphName, subjectId = r.subjectId, data = r.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) });
                }
                index += batchLength;
                complete = index >= ksl.Count;
            }
            return results;
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

        public async Task SetObjectExistence(KnowledgeState ks, string nodeName, List<DarlTime> existence)
        {
            if (_model == null)
                await FetchModel();
            if (!_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            ks.knowledgeGraphName = _graphName;
            var obj = _model.ObjectsByExternalId[nodeName];
            var att = obj.properties.FirstOrDefault(a => a.name == "existence");
            if(att != null)
            {
                att.existence = existence;
            }
            else
            {
                ks.data[obj.id].Add(new GraphAttribute { value = String.Empty, type = GraphAttribute.DataType.TEMPORAL, lineage = "noun:01,5,03,3,018", confidence = 1.0, id = Guid.NewGuid().ToString(), name = "existence", inferred = false, existence = existence });
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
                ks.data[link.id].Add(new GraphAttribute { name = link.name, type = GraphAttribute.DataType.CONNECTION, confidence = link.weight, lineage = link.lineage, id = link.id, inferred = true, value = remoteSubjectId });
            }
            else
            {
                throw new ThinkBaseException($"Connection from {nodeName} to {destName} not found. Schema change?");
            }
        }

        public static List<GraphAttribute> ConvertAttributeInputList(List<GraphAttributeInput> list)
        {
            var l = new List<GraphAttribute>();
            foreach (var a in list)
                l.Add(ConvertAttributeInput(a));
            return l;
        }

        public static GraphAttribute ConvertAttributeInput(GraphAttributeInput a)
        {
            return new GraphAttribute { id = Guid.NewGuid().ToString(), confidence = a.confidence ?? 1.0, inferred = a.inferred ?? false, value = a.value, existence = a.existence, name = a.name, type = a.type, lineage = a.lineage };
        }

        public static List<GraphAttributeInput> ConvertAttributeInputList(List<GraphAttribute> list)
        {
            var l = new List<GraphAttributeInput>();
            foreach (var a in list)
                l.Add(ConvertAttributeInput(a));
            return l;
        }

        public static GraphAttributeInput ConvertAttributeInput(GraphAttribute a)
        {
            return new GraphAttributeInput { confidence = a.confidence, inferred = a.inferred, value = a.value, existence = a.existence, name = a.name, type = a.type, lineage = a.lineage };
        }

        public override string ToString()
        {
            return traceWriter.ToString();
        }

        public async Task<string> ExportNodaModel()
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName},
                Query = @"query ($name: String! ){exportNoda(graphName: $name)}"
            };
            var resp = await client.SendQueryAsync<ExportNodaResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            return resp.Data.exportNoda;
        }

        private KnowledgeStateInput ConvertKnowledgeState(KnowledgeState ks)
        {
            var ksi = new KnowledgeStateInput { knowledgeGraphName = ks.knowledgeGraphName, subjectId = ks.subjectId };
            foreach (var c in ks.data.Keys)
            {
                var l = new List<GraphAttributeInput>();
                foreach (var p in ks.data[c])
                {
                    l.Add(new GraphAttributeInput { confidence = p.confidence, existence = p.existence, inferred = p.inferred, lineage = p.lineage, name = p.name, type = p.type, value = p.value });
                }
                ksi.data.Add(new StringListGraphAttributeInputPair { name = c, value = l });
            }
            return ksi;
        }
    }
}
