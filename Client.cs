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

        public Client(string authcode, string graphName)
        {
            client = new GraphQLHttpClient("https://darl.dev/graphql/", new SystemTextJsonSerializer());
            client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authcode);
            _graphName = graphName;
        }

        public async Task<GraphModel> FetchModel()
        {
            var modelReq = new GraphQLHttpRequest
            {
                Variables = new { name = _graphName },
                Query = "query ($name: String!){kGraphByName(name: $name){name model{vertices{name value{lineage subLineage id externalId properties{lineage name value }}} edges {name value{lineage endId startId name inferred weight}}}}}"
            };
            var model = await client.SendQueryAsync<KGraphResponse>(modelReq);
            return model.Data.kGraphByName.model;
        }

        public async Task<KnowledgeState> GetKnowledgeState(string subjectId)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { name = _graphName, id = subjectId },
                Query = @"($name: String! $id: String!){getKnowledgeState(graphName: $name id: $id){knowledgeGraphName subjectId data{ name value {name type value lineage inferred confidence}}}}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            return resp.Data.getKnowledgeState;
        }

        public async Task<KnowledgeState> CreateKnowledgeState(KnowledgeState ks)
        {
            var req = new GraphQLHttpRequest()
            {
                Variables = new { ks },
                Query = @"mutation ($ks: KnowledgeStateInput!){ createKnowledgeState(ks: $ks ){subjectId }}"
            };
            var resp = await client.SendQueryAsync<KnowledgeStateResponse>(req);
            return resp.Data.createKnowledgeState;
        }
    }
}
