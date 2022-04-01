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

namespace ThinkBase.Client.Tests
{
    [TestClass]
    public class BasicTests
    {
        private string _apiKey;
        private string _path = "https://darl.dev/graphql";

        [TestInitialize()]
        public void Initialize()
        {
            var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BasicTests>()
            .Build();
            _apiKey = configuration["apiKey"];
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
            var newKS = await client.CreateKnowledgeState(ks,false,true);
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
            subs.Subscribe( a => {
                rep = a;
            });
            Thread.Sleep(5000); //allow 5 seconds to run
            await client.FetchModel(); //should work
            Assert.IsTrue(rep != null);
            Assert.IsTrue(rep.trainPerformance > 80.0);
            Assert.IsTrue(await client.DeleteKGraph());
        }
    }
}
