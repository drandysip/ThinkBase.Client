namespace ThinkBase.Client.GraphModels
{
    public class Interaction
    {
        public string darl { get; set; } = string.Empty;

        public string reference { get; set; } = string.Empty;

        public DarlVarResponse? response { get; set; }
    }
}