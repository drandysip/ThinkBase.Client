using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using ThinkBase.Client.GraphModels;

namespace ThinkBase.Client
{
    public class Client : IClient
    {
        private string _graphName;
        private GraphQLHttpClient client;
        private GraphModel? _model;
        private int batchLength { get; set; } = 10;

        ITraceWriter traceWriter = new MemoryTraceWriter();

        public Client(string authcode, string graphName, string path = "https://darl.dev/graphql", bool noSubs = false)
        {
            client = new GraphQLHttpClient(path, new NewtonsoftJsonSerializer(new JsonSerializerSettings
            {
                TraceWriter = traceWriter,
                ContractResolver = new CamelCasePropertyNamesContractResolver { IgnoreIsSpecifiedMembers = true },
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters = { new ConstantCaseEnumConverter() }
            }));
            if (!string.IsNullOrEmpty(authcode))
            {
                client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authcode);
                if (!noSubs)
                {
                    client.Options.ConfigureWebSocketConnectionInitPayload = (GraphQLHttpClientOptions x) =>
                    {//adds authorization for subscriptions
                        return new Dictionary<string, string>()
                        {
                            ["Authorization"] = authcode
                        };
                    };
                }
            }
            _graphName = graphName;
        }


        /// <summary>
        /// Get a read-only version of the graph
        /// </summary>
        /// <returns>The graph model</returns>
        public async Task<GraphModel> FetchModel(bool asSystem = false)
        {
            GraphQLResponse<KGraphResponse>? model = null;
            try
            {
                var modelReq = new GraphQLHttpRequest
                {
                    Variables = new { name = _graphName, ass = asSystem },
                    Query = "query ($name: String! $ass: Boolean){kGraphByName(name: $name asSystem: $ass){name model{vertices{name value{lineage subLineage id externalId properties{lineage name value type existence {raw precision } } existence {raw precision }}} edges {name value{lineage endId startId name inferred weight id existence {raw precision }}}}}}"
                };
                model = await client.SendQueryAsync<KGraphResponse>(modelReq);
            }
            catch(Exception ex)
            {
                throw new Exception($"FetchModel failed. Message: {ex.Message}");
            }
            if (model == null) 
                throw new Exception("SendQueryAsync returned null.");
            if (model.Errors != null && model.Errors.Count() > 0)
                throw new Exception(model.Errors[0].Message);
            if(model.Data.kGraphByName == null)
                throw new Exception($"{_graphName} is not present in this account.");
            _model = model.Data.kGraphByName.model ?? new GraphModel();
            _model.Init();
            return _model;
        }

        public async Task<KnowledgeState?> GetKnowledgeState(string subjectId)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName, id = subjectId },
                Query = @"query ($name: String! $id: String!){getKnowledgeState(graphName: $name id: $id){knowledgeGraphName subjectId created data{ name value {name type value lineage inferred confidence}}}}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            var ksi =  resp.Data.getKnowledgeState;
            if(ksi != null)
                return new KnowledgeState { knowledgeGraphName = _graphName, subjectId = subjectId, data = ksi.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) };
            return null;
        }

        /// <summary>
        /// Get a set of knowledge states containing data/state for the given object.
        /// </summary>
        /// <param name="parentId">The id of the object the KSs must contain</param>
        /// <param name="asSystem">Admin accounts only</param>
        /// <returns>A list of KnowledgeStates</returns>
        /// <exception cref="Exception">Thrown for query failure</exception>
        public async Task<List<KnowledgeState>> GetChildKnowledgeStates(string parentId, bool asSystem)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName, id = parentId, ass = asSystem },
                Query = @"query ($name: String! $id: String! $ass: Boolean){getKnowledgeStatesByType(graphName: $name typeObjectId: $id asSystem: $ass){knowledgeGraphName created subjectId data{ name value {name type value lineage inferred confidence}}}}"
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
                    list.Add(new KnowledgeState { knowledgeGraphName = _graphName, subjectId = k.subjectId, data = k.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) });
                }
            }
            return list;
        }

        public async Task<List<KnowledgeState>> GetChildKnowledgeStatesWithAttributeValue(string parentId, string attLineage, string attValue, bool asSystem)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName, id = parentId, ass = asSystem, attl = attLineage, attv = attValue },
                Query = @"query ($name: String! $id: String! $attl: String! $attv: String! $ass: Boolean){getKnowledgeStatesByTypeAndAttribute(graphName: $name typeObjectId: $id attLineage: $attl attValue: $attv asSystem: $ass){knowledgeGraphName created subjectId data{ name value {name type value lineage inferred confidence}}}}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            var ksi = resp.Data.getKnowledgeStatesByTypeAndAttribute;
            var list = new List<KnowledgeState>();
            if (ksi != null)
            {
                foreach (var k in ksi)
                {
                    list.Add(new KnowledgeState { knowledgeGraphName = _graphName, subjectId = k.subjectId, data = k.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) });
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
        public async Task<KnowledgeState?> CreateKnowledgeState(KnowledgeState ks, bool? asSystem, bool transient = false)
        {
            var ksi = ConvertKnowledgeState(ks,transient);
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
            if (ksinew != null)
                return new KnowledgeState { knowledgeGraphName = _graphName, subjectId = ksinew.subjectId, data = ksinew.data.ToDictionary(a => a.name, b => ConvertAttributeInputList(b.value)) };
            return null;
        }

        /// <summary>
        /// Add a list of Knowledge States to the graph or overwrite existing, using batching to increase performance.
        /// </summary>
        /// <param name="ks"></param>
        /// <returns></returns>
        public async Task<List<KnowledgeState>> CreateKnowledgeStateBatched(List<KnowledgeState> ksl, bool transient = false)
        {
            bool complete = false;
            int index = 0;
            var ksis = new List<KnowledgeStateInput>();
            var results = new List<KnowledgeState>();
            while(!complete)
            {
                for(int n = 0; n < batchLength && n + index < ksl.Count; n++)
                {
                    ksis.Add(ConvertKnowledgeState(ksl[n + index],transient));
                }
                var req = new GraphQLHttpRequest()
                {
                    Variables = new { ks = ksis },
                    Query = @"mutation ($ks: [knowledgeStateInput]!){ createKnowledgeStateList(ksl: $ks ){knowledgeGraphName subjectId transient data{ name value {name type value lineage inferred confidence}}}}"
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
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            ks.knowledgeGraphName = _graphName;
            var obj = _model.ObjectsByExternalId[nodeName];
            bool found = false;
            foreach (var l in obj.properties ?? new List<GraphAttribute>())
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

        public async Task<GraphAttribute?> GetGraphAttributeByLineage(KnowledgeState ks, string nodeName, string lineage)
        {
            if (_model == null)
                await FetchModel();
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            var obj = _model.ObjectsByExternalId[nodeName];
            if(ks.data.ContainsKey(obj.id))
            {
                return ks.data[obj.id].FirstOrDefault(a => a.lineage.StartsWith(lineage));
            }
            return null;
        }

        public async Task<GraphAttribute?> GetGraphAttributeByAttName(KnowledgeState ks, string nodeName, string attName)
        {
            if (_model == null)
                await FetchModel();
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            var obj = _model.ObjectsByExternalId[nodeName];
            if (ks.data.ContainsKey(obj.id))
            {
                return ks.data[obj.id].FirstOrDefault(a => a.name == attName);
            }
            return null;
        }

        public async Task SetObjectExistence(KnowledgeState ks, string nodeName, List<DarlTime> existence)
        {
            if (_model == null)
                await FetchModel();
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            ks.knowledgeGraphName = _graphName;
            var obj = _model.ObjectsByExternalId[nodeName];
            if(obj == null)
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");
            var att = obj.properties?.FirstOrDefault(a => a.name == "existence");
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
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(nodeName))
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
            return new GraphAttributeInput { confidence = a.confidence, inferred = a.inferred, value = a.value ?? "", existence = a.existence, name = a.name, type = a.type, lineage = a.lineage };
        }

        public override string ToString()
        {
            return traceWriter.ToString() ?? "";
        }

        public async Task<string?> ExportNodaModel()
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName},
                Query = @"query ($name: String! ){exportNoda(graphName: $name)}"
            };
            var resp = await client.SendQueryAsync<ExportNodaResponse>(req);
            if (resp == null || (resp.Errors != null && resp.Errors.Count() > 0))
                throw new Exception(resp?.Errors?[0].Message);
            return resp.Data.exportNoda;
        }

        /// <summary>
        /// Admin only
        /// </summary>
        /// <param name="collateral"></param>
        /// <param name="subject"></param>
        /// <param name="sendfrom"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        public async Task<int?> MailShot(string collateral, string subject, string sendfrom, bool test)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { collateral = collateral, subject = subject, sendfrom = sendfrom, test = test },
                Query = @"mutation ($collateral: String! $subject: String! $sendfrom: String! $test: Boolean!  ){mailshot(collateral: $collateral subject: $subject sendfrom: $sendfrom test: $test)}"
            };
            var resp = await client.SendQueryAsync<MailShotResponse>(req);
            if (resp == null || (resp.Errors != null && resp.Errors.Count() > 0))
                throw new Exception(resp?.Errors?[0].Message);
            return resp.Data.mailshot;
        }

        private KnowledgeStateInput ConvertKnowledgeState(KnowledgeState ks, bool transient = false)
        {
            var ksi = new KnowledgeStateInput { knowledgeGraphName = ks.knowledgeGraphName, subjectId = ks.subjectId };
            foreach (var c in ks.data.Keys)
            {
                var l = new List<GraphAttributeInput>();
                foreach (var p in ks.data[c])
                {
                    l.Add(new GraphAttributeInput { confidence = p.confidence, existence = p.existence, inferred = p.inferred, lineage = p.lineage, name = p.name, type = p.type, value = p.value ?? "" });
                }
                ksi.data.Add(new StringListGraphAttributeInputPair { name = c, value = l });
            }
            return ksi;
        }

        public async Task<IObservable<KnowledgeStateInput>> SubscribeToSeek(string nodeName)
        {
            if (_model == null)
                await FetchModel();
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(nodeName))
                throw new ArgumentOutOfRangeException($"{nodeName} not found in {_graphName}");

            var req = new GraphQLRequest()
            {
                Variables = new { name = _graphName, target = _model.ObjectsByExternalId[nodeName].id },
                Query = @"subscription ($name: String! $target: String!){graphChanged(graphName: $name target: $target){knowledgeGraphName created subjectId data{ name value {name type value lineage inferred confidence}}}}"
            };
            IObservable<GraphQLResponse<GraphChangedResult>> subscriptionStream = client.CreateSubscriptionStream<GraphChangedResult>(req);
            ISubject<KnowledgeStateInput> _knowledgeStateStream = new ReplaySubject<KnowledgeStateInput>(1);
            subscriptionStream.Subscribe( x =>
            {               
                _knowledgeStateStream.OnNext(x.Data.graphChanged);
            });
            return _knowledgeStateStream.AsObservable();
        }

        public IObservable<DarlMineReport> SubscribeToBuild(string name, string? data, string? patternPath, List<DataMap> dataMaps )
        {
            var req = new GraphQLRequest()
            {
                Variables = new { name = name, data = data, patternPath = patternPath, dataMaps = dataMaps },
                Query = @"subscription ($name: String! $data: String! $patternPath: String! $dataMaps: [dataMap]! ){build(name: $name data: $data patternPath: $patternPath dataMaps: $dataMaps){code errorText testPerformance trainPercent trainPerformance unknownResponsePercent}}"
            };
            IObservable<GraphQLResponse<BuildResult>> subscriptionStream = client.CreateSubscriptionStream<BuildResult>(req);
            ISubject<DarlMineReport> reportStream = new ReplaySubject<DarlMineReport>(1);
            subscriptionStream.Subscribe(x =>
            {
                reportStream.OnNext(x.Data.build);
            });
            return reportStream.AsObservable();
        }

        public async Task<List<Interaction>> Interact(string conversationId, string message)
        {
            var req = new GraphQLHttpRequest() { Variables = new { name = _graphName, ksid = conversationId, text = message, messageName = "message" }, Query = @"query ($name: String! $ksid: String! $text:  String! $messageName: String!){interactKnowledgeGraph(kgModelName: $name conversationId: $ksid conversationData: { dataType: textual name: $messageName value: $text }){ darl reference response{dataType name value categories{name value }}}}" };
            var resp = await client.SendQueryAsync<InteractResponse>(req);
            if (resp.Errors != null && resp.Errors.Length > 0)
            {
                throw new ThinkBaseException($"Interact error: {resp.Errors[0].Message}");
            }
            return resp.Data.interactKnowledgeGraph;
        }

        public async Task<string> GetObjectIdFromName(string name)
        {
            if (_model == null)
                await FetchModel();
            if (_model == null || !_model.ObjectsByExternalId.ContainsKey(name))
                throw new ArgumentOutOfRangeException($"{name} not found in {_graphName}");
            return _model.ObjectsByExternalId[name].id;
        }

        public async Task<bool> DeleteKGraph()
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName },
                Query = @"mutation ($name: String!){deleteKG(name: $name)}"
            };
            var resp = await client.SendQueryAsync<object>(req);
            if (resp.Errors != null && resp.Errors.Count() > 0)
                throw new Exception(resp.Errors[0].Message);
            return ((JObject)resp.Data).Value<bool>("deleteKG");
        }
    }
}
