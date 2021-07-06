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
        /// Creates a blank KS with the appropriate attributes and links ready to fill in.
        /// </summary>
        /// <param name="externalId"></param>
        /// <returns>A blank KS.</returns>
        public async Task<KnowledgeState> GetKnowledgeStateBlank(string externalId)
        {
            if (_model == null)
                await FetchModel();
            if (!_model.ObjectsByExternalId.ContainsKey(externalId))
                throw new ArgumentOutOfRangeException($"{externalId} not found in {_graphName}");
            var ks = new KnowledgeState { knowledgeGraphName = _graphName, subjectId = Guid.NewGuid().ToString() };
            var obj = _model.ObjectsByExternalId[externalId];
            obj.properties.ForEach(a => a.value = "");
            ks.data.Add(obj.id, obj.properties);
            var links = _model.Connections.Values.Where(a => a.startId == obj.id && a.inferred);
            foreach(var c in links)
            {
                ks.data.Add(c.id, new List<GraphAttribute> { new GraphAttribute { name = c.name, type = GraphAttribute.DataType.connection, confidence = c.weight, lineage = c.lineage, id = c.id, inferred = true } });
            }
            return ks;
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
                ksi.data.Add(new StringListGraphAttributePair { Name = c, Value = ks.data[c] });
            }
            var req = new GraphQLHttpRequest()
            {
                Variables = new { ks = ksi },
                Query = @"mutation ($ks: KnowledgeStateInput!){ createKnowledgeState(ks: $ks ){subjectId }}"
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
    }
}
