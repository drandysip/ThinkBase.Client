using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using ThinkBase.Client.GraphModels;

namespace ThinkBase.Client.StressTest
{
    internal class Program
    {
        private static string _apiKey;
        private static string _path = "https://localhost:44311/graphql";
        private static string _adminApiKey;
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

            _apiKey = configuration["apiKey"];
            _adminApiKey = configuration["adminApiKey"];
            Console.WriteLine("Starting stress test with 5, 100 step interactions.");
            var start = DateTime.Now;
            await Task.WhenAll(
                    TestInteract(),
                    TestInteract(),
                    TestInteract(),
                    TestInteract(),
                    TestInteract()
                );
            Console.WriteLine($"Duration {(DateTime.Now - start).TotalSeconds} seconds");
            Console.Read();
        }

        public static async Task TestInteract()
        {
            var graph = "personality_test.graph";
            var client = new Client(_apiKey, graph, _path);
            var res = await client.FetchModel();
            res.Init();
            var conversationId = Guid.NewGuid().ToString();
            var response = await client.Interact(conversationId, "What is my personality?");
            response = await client.Interact(conversationId, "Yes");
            for (int i = 0; i < 100; i++)
            {
                if (response[0].response.dataType == DarlVarResponse.DataType.categorical)
                    response = await client.Interact(conversationId, response[0].response.categories[0].name);
                else if (response[0].response.dataType == DarlVarResponse.DataType.numeric)
                    response = await client.Interact(conversationId, "50");
            }
            response = await client.Interact(conversationId, response[0].response.categories[0].name); //last question
            //fetch the KS
            var ks = await client.GetInteractKnowledgeState(conversationId);
        }
    }
}
