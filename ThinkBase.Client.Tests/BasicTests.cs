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
        private string _path = "https://localhost:44311/graphql"; //"https://darlgraphql-stagng.azurewebsites.net/graphql";//  "https://darl.dev/graphql"; 
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
            Assert.IsTrue(response[1].response.dataType == DarlVarResponse.DataType.numeric);
            response = await client.Interact(conversationId, "50");
            for (int i = 0; i < 100; i++)
            {
                if(response[0].response.dataType == DarlVarResponse.DataType.categorical)
                    response = await client.Interact(conversationId, response[0].response.categories[0].name);
                else if(response[0].response.dataType == DarlVarResponse.DataType.numeric)
                    response = await client.Interact(conversationId, "50");
            }
            Assert.AreEqual("# Results\nIn percentiles\n\nPsychoticness: 90.15\n\nNeuroticness: 61.76\n\nExtraversion: 94.40\n\nSelf-Deception: 29.91", response[0].response.value);
            //fetch the KS
            var ks = await client.GetInteractKnowledgeState(conversationId);
            Assert.IsNotNull(ks);
            Assert.AreEqual(85,ks.data.Count); //check why only 85
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
            Assert.IsTrue(response[1].response.dataType == DarlVarResponse.DataType.numeric);
            response = await client.Interact(conversationId, "50");
            for (int i = 0; i < 100; i++)
            {
                if (response[0].response.dataType == DarlVarResponse.DataType.categorical)
                    response = await client.Interact(conversationId, response[0].response.categories[0].name);
                else if (response[0].response.dataType == DarlVarResponse.DataType.numeric)
                    response = await client.Interact(conversationId, "50");
            }
            Assert.AreEqual("# Results\nIn percentiles\n\nPsychoticness: 90.15\n\nNeuroticness: 61.76\n\nExtraversion: 94.40\n\nSelf-Deception: 29.91", response[0].response.value);
            //fetch the KS
            var ks = await client.GetInteractKnowledgeState(conversationId);
            Assert.IsNotNull(ks);
            Assert.AreEqual(85, ks.data.Count);
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
            Assert.IsTrue((duration1 > duration2));//only true if cache doesn't contain nodaView result.
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

        [TestMethod]
        public async Task TestKGraphCreateDelete()
        {
            var graph = "Test.graph";
            var client = new Client(_apiKey, graph, _path);
            Assert.IsTrue(await client.CreateKGraph());
            var res = await client.FetchModel();
            Assert.IsTrue(await client.DeleteKGraph());
        }
        [TestMethod]
        public async Task TestCreateObjectsAndConnections()
        {
            var graph = "Test.graph";
            var client = new Client(_apiKey, graph, _path);
            Assert.IsTrue(await client.CreateKGraph());
            var res = await client.FetchModel();
            Assert.AreEqual(0, res.vertices.Count());
            Assert.AreEqual(0, res.edges.Count());
            var obj1 = await client.CreateGraphObject(new GraphObject { name = "obj1", externalId = "obj1", lineage = "noun:01,0,2,00,38,00,06,1" });
            var obj2 = await client.CreateGraphObject(new GraphObject { name = "obj2", externalId = "obj2", lineage = "noun:01,0,2,00,38,00,06,1" });
            var obj3 = await client.CreateGraphObject(new GraphObject { name = "obj3", externalId = "obj3", lineage = "noun:01,0,2,00,38,00,06,1" });
            var obj4 = await client.CreateGraphObject(new GraphObject { name = "obj4", externalId = "obj4", lineage = "noun:01,0,2,00,38,00,06,1" });
            var conn1 = await client.CreateGraphConnection(new GraphConnection { name = "connects to", weight = 1.0, startId = obj1.id, endId = obj2.id, lineage = "verb:360,09" });
            var conn2 = await client.CreateGraphConnection(new GraphConnection { name = "connects to", weight = 1.0, startId = obj2.id, endId = obj3.id, lineage = "verb:360,09" });
            var conn3 = await client.CreateGraphConnection(new GraphConnection { name = "connects to", weight = 1.0, startId = obj3.id, endId = obj4.id, lineage = "verb:360,09" });
            var conn4 = await client.CreateGraphConnection(new GraphConnection { name = "connects to", weight = 1.0, startId = obj4.id, endId = obj1.id, lineage = "verb:360,09" });
            res = await client.FetchModel();
            Assert.AreEqual(4, res.vertices.Count());
            Assert.AreEqual(4, res.edges.Count());
            Assert.IsTrue(await client.DeleteKGraph());
        }

        [TestMethod]
        public async Task AttributeManipulationTest()
        {
            var graph = "Test.graph";
            var client = new Client(_apiKey, graph, _path);
            Assert.IsTrue(await client.CreateKGraph());
            var res = await client.FetchModel();
            Assert.AreEqual(0, res.vertices.Count());
            Assert.AreEqual(0, res.edges.Count());
            var obj1 = await client.CreateGraphObject(new GraphObject { name = "obj1", externalId = "obj1", lineage = "noun:01,0,2,00,38,00,06,1" });
            await client.AddAttributeByName(obj1.id, new GraphAttribute { confidence = 1.0, name = "fred", lineage = "noun:01,0,2,00,38,00,06,1", value = "poops" });
            res = await client.FetchModel();
            var obj2 = await client.GetObjectById(obj1.id);
            Assert.AreEqual(1, obj2.properties.Count);
            await client.DeleteAttributeByName(obj1.id, "fred");
            res = await client.FetchModel();
            obj2 = await client.GetObjectById(obj1.id);
            Assert.AreEqual(0, obj2.properties.Count);
            await client.AddAttributeByName(obj1.id, new GraphAttribute { confidence = 1.0, name = "fred", lineage = "noun:01,0,2,00,38,00,06,1", value = "poops" });
            await client.AddSubAttributeByName(obj1.id, "fred", new GraphAttribute { confidence = 1.0, name = "bill", lineage = "noun:01,0,2,00,38,00,06,1", value = "poops" });
            res = await client.FetchModel();
            obj2 = await client.GetObjectById(obj1.id);
            Assert.AreEqual(1, obj2.properties.Count);
            Assert.AreEqual(1, obj2.properties[0].properties.Count);
            Assert.IsTrue(await client.DeleteKGraph());
        }

        [TestMethod]
        public async Task downloadTest()
        {
            var graph = "Cocomo_II.graph";
            var client = new Client(_apiKey, graph, _path);
            var graphBlob = await client.DownloadGraph();
            //            File.WriteAllBytes(graph, graphBlob);
            Assert.IsTrue(graphBlob.Count() > 70000);
        }

        [TestMethod]
        public async Task uploadTest()
        {
            var graph = Guid.NewGuid().ToString() + ".graph";
            var client = new Client(_apiKey, graph, _path);
            var exists = await client.TempKGExists();
            Assert.IsFalse(exists);
            var reader = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ThinkBase.Client.Tests.Cocomo_II.graph"));
            var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
            var r = await client.UploadGraph(bytes);
            exists = await client.TempKGExists();
            Assert.IsTrue(exists);
            //don't need to delete.
        }

        [TestMethod]
        public async Task UploadBadDataTest()
        {
            var graph = Guid.NewGuid().ToString() + ".graph";
            var client = new Client(_apiKey, graph, _path);
            var exists = await client.TempKGExists();
            Assert.IsFalse(exists);
            var reader = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ThinkBase.Client.Tests.iris_data.xml"));
            var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
            var r = await client.UploadGraph(bytes);
            Assert.AreEqual("Not a valid Graph.", r);
        }
    }
}
