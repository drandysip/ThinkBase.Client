using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class DarlTime
    {
        public double raw { get; set; }
        public double precision { get; set; }

        public static readonly DateTime yearZero = new DateTime(1, 1, 1, 0, 0, 0);

        public static readonly double secondsPerYear = 31556952.0;

        public static readonly DarlTime MaxValue = new DarlTime(DateTime.MaxValue);

        public static readonly DarlTime MinValue = new DarlTime(-MaxValue.raw);

        public static DarlTime UtcNow { get { return new DarlTime(DateTime.UtcNow); } }

        public DarlTime()
        {
            raw = 0.0;
        }

        public DarlTime(DateTime dt)
        {
            raw = (double)(dt - yearZero).TotalSeconds;
        }

        public DarlTime(double d)
        {
            raw = d;
        }

        public override string ToString()
        {
            if (raw >= 0)
            {
                DateTime dateTime =  raw > 0.0 ? yearZero + TimeSpan.FromSeconds((double)this.raw) : DateTime.MinValue;
                return dateTime.ToString();
            }
            else
            {
                if (raw > -secondsPerYear)
                    return "0 AD";
                return $"{Math.Abs(Math.Truncate(raw / secondsPerYear))} BC";
            }
        }
    }
}
