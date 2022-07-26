namespace ThinkBase.Client.GraphModels
{
    public class KGraphResponse
    {
        public KGraph kGraphByName { get; set; }
        public byte[] KGContents { get; set; }
        public bool tempKGExists { get; set; }
    }
}
