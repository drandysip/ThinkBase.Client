# ThinkBase.Client
ThinkBase .net 5 client
This is a simple client for accessing, inferring from and creating ThinkBase models on the SaaS site [darl.dev](https://darl.dev).
ThinkBase is a Knowledge Graph inference engine.
Read more about ThinkBase at [the documentation](https://darl.dev/docs#/).

The most recent nuget package can be found [here](https://www.nuget.org/packages/ThinkBase.Client/).

# Usage

Create a client with your ThinkBase API key and the name of the graph
```C#
var client = new Client("<API key>", "graphName");
var topLevelModel = await client.FetchModel();
topLevelModel.Init();
```
Create some knowledge states and link them. In this case "c" is a country object with name and id. "m" is a monarch with name and id and a link to a country ks via its subject ID.
```C#
var countryKS = new KnowledgeState();
await client.SetDataValue(countryKS, "country", "name", c.Name);
await client.SetDataValue(countryKS, "country", "id", c.Id.ToString());
countryKS.subjectId = "country:" + c.Id.ToString();
await client.CreateKnowledgeState(countryKS);
var monarchKS = new KnowledgeState();
await client.SetDataValue(monarchKS, "monarch", "name", m.Name);
await client.SetDataValue(monarchKS, "monarch", "id", m.Id.ToString());
await client.SetConnectionPresence(monarchKS, "monarch", "country", "country:" + m.CountryId.ToString());
monarchKS.subjectId = "monarch:" + m.Id.ToString();
await client.CreateKnowledgeState(monarchKS);
```
In each case above, if the "country" or "monarch" objects in the graph don't contain the matching properties an exception is thrown.
Similarly, the SetConnectionPresence call checks to see if a connection exists between the two nodes described. 
Knowledge states are linked by subject ID. If a subjectId specified in a link does not exist no error is thrown at runtime but the path described will be ignored.

You can fetch a knowledge state by its subject Id.

```C#
var customerKS = await client.GetKnowledgeState("customer:" + customerId.ToString());
```
