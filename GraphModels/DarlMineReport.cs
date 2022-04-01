namespace ThinkBase.Client.GraphModels
{
    public class DarlMineReport
    {
        public string? code { get; set; }
        public string? errorText { get; set; }
        public double? testPerformance { get; set; }
        public double? trainPerformance { get; set; }
        public double? unknownResponsePercent { get; set; }
        public int? trainPercent { get; set; }
    }
}