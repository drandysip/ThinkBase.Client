namespace ThinkBase.Client.GraphModels
{
    public class GraphObjectResponse
    {
        public GraphObject? getGraphObjectByExternalId { get; set; }
        public GraphObject? createGraphObject { get; set; }
        public GraphObject? updateGraphObject { get; set; }

    }
}
