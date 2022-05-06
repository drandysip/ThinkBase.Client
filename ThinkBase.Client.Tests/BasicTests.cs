using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ThinkBase.Client.GraphModels;

//[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace ThinkBase.Client.Tests
{
    [TestClass]
    public class BasicTests
    {
        private string _apiKey;
        private string _path = "https://darl.dev/graphql"; // "https://localhost:44311/graphql"; 
        private string _adminApiKey;

        [TestInitialize()]
        public void Initialize()
        {
            var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BasicTests>()
            .Build();
            _apiKey = configuration["apiKey"];
            _adminApiKey = configuration["adminApiKey"];
        }

        [TestMethod]
        public async Task TestCreateKS()
        {
            var client = new Client(_apiKey, "ai_triage.graph", _path);
            var res = await client.FetchModel();
            res.Init();
            var ks = new KnowledgeState();
            await client.SetDataValue(ks, "rapid_change", "text", "poopies");
            await client.SetConnectionPresence(ks, "rapid_change", "rapid_change_yes", "poopiess");
            ks.subjectId = "kjgjkhkjhkjhk";
            var newKS = await client.CreateKnowledgeState(ks, false, true);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task TestGetKnowledgeStateBadModel()
        {
            var client = new Client(_apiKey, "stamps2.graph");
            var res = await client.FetchModel();
            res.Init();
            var ks = await client.GetKnowledgeState("customer:495944");
        }

        [TestMethod]
        public async Task TestGetKnowledgeState()
        {
            var client = new Client(_apiKey, "ai_triage.graph", _path);
            var res = await client.FetchModel();
            res.Init();
            var ks = await client.GetKnowledgeState("customer:495944");
        }
        [TestMethod]
        public async Task TestGetNoda()
        {
            var client = new Client(_apiKey, "ai_triage.graph", _path);
            var res = await client.FetchModel();
            res.Init();
            var m = await client.ExportNodaModel();
            File.WriteAllText("ai_triage_noda.json", m);
        }

        [TestMethod]
        public async Task TestBuild()
        {
            var newGraph = "TestGraph.graph";
            var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ThinkBase.Client.Tests.iris_data.xml"));
            var client = new Client(_apiKey, newGraph, _path);
            var subs = client.SubscribeToBuild(newGraph, await reader.ReadToEndAsync(), "/irisdata/Iris", new List<DataMap> {new DataMap{
                objId = "sepal_length",
                dataType = GraphAttribute.DataType.NUMERIC,
                relPath = "sepal_length",
                objectLineage = "appraisal"
            },
            new DataMap{
                objId = "sepal_width",
                dataType = GraphAttribute.DataType.NUMERIC,
                relPath = "sepal_width",
                objectLineage = "appraisal"
            },
            new DataMap{
                objId = "petal_length",
                dataType = GraphAttribute.DataType.NUMERIC,
                relPath = "petal_length",
                objectLineage = "appraisal",
            },
            new DataMap{
                objId = "petal_width",
                dataType = GraphAttribute.DataType.NUMERIC,
                relPath = "petal_width",
                objectLineage = "appraisal",
            },
            new DataMap{
                objId = "class",
                dataType = GraphAttribute.DataType.CATEGORICAL,
                relPath = "class",
                objectLineage = "noun:00,1,00,1,0,06,26,18,0,0",
                objectSubLineage = "noun:01,2,04,2,21",
                target = true
            }});
            DarlMineReport? rep = null;
            subs.Subscribe(a =>
            {
                rep = a;
            });
            Thread.Sleep(5000); //allow 5 seconds to run
            await client.FetchModel(); //should work
            Assert.IsTrue(rep != null);
            Assert.IsTrue(rep.trainPerformance > 70.0);
            Assert.IsTrue(await client.DeleteKGraph());
        }

        [TestMethod]
        public async Task TestSeek()
        {
            var graph = "backoffice_new.graph";
            var client = new Client(_apiKey, graph, _path);
            var res = await client.FetchModel();
            res.Init();
            var subs = await client.SubscribeToSeek("newsitem");
            KnowledgeStateInput? knowledgeState = null;
            string errorMessage;
            subs.Subscribe(
                a => knowledgeState = a,
                b => errorMessage = b.Message
                );
            var ks = new KnowledgeState();
            await client.SetDataValue(ks, "newsitem", "title", "A news story");
            await client.SetDataValue(ks, "newsitem", "content", "The content of a news story");
            await client.SetDataValue(ks, "newsitem", "category", "news");
            ks.subjectId = Guid.NewGuid().ToString();
            var newKS = await client.CreateKnowledgeState(ks, false, true);
            Thread.Sleep(1000); //allow 1 second to run
            Assert.IsTrue(knowledgeState != null);
            Assert.IsTrue(knowledgeState.data.Any(a => a.value.Any(b => b.name == "completed")));
        }

        [TestMethod]
        public async Task TestGetChildKnowledgeStates()
        {
            var graph = "backoffice_test.graph";
            var client = new Client(_adminApiKey, graph, _path);
            var res = await client.FetchModel(true);
            res.Init();
            var pushObjectId = await client.GetObjectIdFromName("pushSubscription");
            var subs = await client.GetChildKnowledgeStates(pushObjectId,true);
            Assert.IsNotNull(subs);
            Assert.IsTrue(subs.Any());
        }

        [TestMethod]
        public async Task TestInteract()
        {
            var graph = "personality_test.graph";
            var client = new Client(_apiKey, graph, _path);
            var res = await client.FetchModel();
            res.Init();
            var conversationId = Guid.NewGuid().ToString();
            var response = await client.Interact(conversationId, "What is my personality?");
            Assert.IsNotNull(response);
            Assert.AreEqual(response.Count(), 2);
            Assert.IsTrue(response[0].response.dataType == DarlVarResponse.DataType.textual);
            Assert.IsTrue(response[1].response.dataType == DarlVarResponse.DataType.categorical);
            Assert.IsTrue(response[1].response.categories.Count == 2);
            Assert.IsTrue(response[1].response.categories[0].name == "Yes");
            response = await client.Interact(conversationId, "Yes");
            for (int i = 0; i < 100; i++)
            {
                if(response[0].response.dataType == DarlVarResponse.DataType.categorical)
                    response = await client.Interact(conversationId, response[0].response.categories[0].name);
                else if(response[0].response.dataType == DarlVarResponse.DataType.numeric)
                    response = await client.Interact(conversationId, "50");
            }
            response = await client.Interact(conversationId, response[0].response.categories[0].name); //last question
            Assert.AreEqual("# Results\nIn percentiles\n\nPsychoticness: 90.15\n\nNeuroticness: 97.61\n\nExtraversion: 94.40\n\nSelf-Deception: 29.91", response[0].response.value);
            //fetch the KS
            var ks = await client.GetInteractKnowledgeState(conversationId);
            Assert.IsNotNull(ks);
            Assert.AreEqual(110,ks.data.Count);
        }

        [TestMethod]
        public async Task TestgetKnowledgeStatesByTypeAndAttribute()
        {
            var graph = "backoffice_test.graph";
            var client = new Client(_adminApiKey, graph, _path);
            var res = await client.FetchModel(true);
            res.Init();
            var nItemObjectId = await client.GetObjectIdFromName("pushSubscription");
            var items = await client.GetChildKnowledgeStatesWithAttributeValue(nItemObjectId, "noun:01,4,04,01,01,1,00", "92.27.229.0", true);
            Assert.IsNotNull(items);
            Assert.AreEqual(2, items.Count);
        }

        [TestMethod]
        public async Task TestMailShot()
        {
            var graph = "backoffice_test.graph";
            var client = new Client(_adminApiKey, graph, _path);
            var res = await client.FetchModel(true);
            res.Init();
            var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ThinkBase.Client.Tests.email_template.html"));
            var content = reader.ReadToEnd();
            var count = await client.MailShot(content, "test_email", "support@darl.ai", true);
            Assert.IsTrue(count > 0);
        }

        [TestMethod]
        public async Task StressTestInteract()
        {
            await Task.WhenAll(
                    TestInteract(),
                    TestInteract(),
                    TestInteract()
                    );
        }

        [TestMethod]
        public async Task TestInteractComplete()
        {
            var graph = "personality_test.graph";
            var client = new Client(_apiKey, graph, _path);
            var res = await client.FetchModel();
            res.Init();
            var subs = client.SubscribeToInteractComplete("personality");
            KnowledgeStateInput? knowledgeState = null;
            string errorMessage;
            subs.Subscribe(
                a => knowledgeState = a,
                b => errorMessage = b.Message
                );
            var conversationId = Guid.NewGuid().ToString();
            var response = await client.Interact(conversationId, "What is my personality?");
            Assert.IsNotNull(response);
            Assert.AreEqual(response.Count(), 2);
            Assert.IsTrue(response[0].response.dataType == DarlVarResponse.DataType.textual);
            Assert.IsTrue(response[1].response.dataType == DarlVarResponse.DataType.categorical);
            Assert.IsTrue(response[1].response.categories.Count == 2);
            Assert.IsTrue(response[1].response.categories[0].name == "Yes");
            response = await client.Interact(conversationId, "Yes");
            for (int i = 0; i < 100; i++)
            {
                if (response[0].response.dataType == DarlVarResponse.DataType.categorical)
                    response = await client.Interact(conversationId, response[0].response.categories[0].name);
                else if (response[0].response.dataType == DarlVarResponse.DataType.numeric)
                    response = await client.Interact(conversationId, "50");
            }
            response = await client.Interact(conversationId, response[0].response.categories[0].name); //last question
            Assert.AreEqual("# Results\nIn percentiles\n\nPsychoticness: 90.15\n\nNeuroticness: 97.61\n\nExtraversion: 94.40\n\nSelf-Deception: 29.91", response[0].response.value);
            //fetch the KS
            var ks = await client.GetInteractKnowledgeState(conversationId);
            Assert.IsNotNull(ks);
            Assert.AreEqual(110, ks.data.Count);
            Thread.Sleep(1000); //allow 1 second to run
            Assert.IsTrue(knowledgeState != null);
            Assert.IsTrue(knowledgeState.data.Any(a => a.value.Any(b => b.name == "completed")));

        }

        [TestMethod]
        public async Task TestNodaViewCaching()
        {
            var graph = "personality_test.graph";
            var client = new Client(_apiKey, graph, _path);
            var res = await client.FetchModel();
            res.Init();
            var start = DateTime.UtcNow;
            var content = await client.NodaView();
            var duration1 = DateTime.UtcNow - start;
            Assert.IsNotNull(content);
            start = DateTime.UtcNow;
            content = await client.NodaView();
            var duration2 = DateTime.UtcNow - start;
            Assert.IsTrue((duration1 > duration2));
        }

        [TestMethod]
        public async Task TestRegisterForMarketing()
        {
            var graph = "backoffice_test.graph";
            var client = new Client(_adminApiKey, graph, _path);
            var res = await client.FetchModel(true);
            res.Init();
            var resp = await client.RegisterForMarketing("andy", "andy@darlpolicy.com", "192.168.1.1", "-0.6", "54");
            Assert.IsNotNull(resp);
            var nItemObjectId = await client.GetObjectIdFromName("person");
            var states = await client.GetChildKnowledgeStatesWithAttributeValue(nItemObjectId, "noun:01,0,2,00,38,00,06,1", "andy@darlpolicy.com",true);
            Assert.IsNotNull(states);
            Assert.AreEqual(1, states.Count());
            var deleted = await client.DeleteKnowledgeState(states[0].subjectId, true);
            Assert.IsNotNull(deleted);
            var old = await client.GetKnowledgeState(states[0].subjectId);
            Assert.IsNull(old);

        }
    }
}
