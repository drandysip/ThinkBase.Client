using System;

namespace ThinkBase.Client.GraphModels
{
    public class DarlTimeInput
    {
        public enum Season { WINTER, SPRING, SUMMER, FALL }
        public double? raw { get; set; }
        public DateTimeOffset dateTimeOffset { get; set; }
        public double? precision { get; set; }
        public int? year { get; set; }
        public Season? season { get; set; }
    }
}

