using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KGraph
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DarlTime? fixedTime { get; set; }
        public string userId { get; set; }
        public bool Shared { get; set; } = false;
        public string OwnerId { get; set; }
        public bool? ReadOnly { get; set; } = false;
        public string InitialText { get; set; }
        public bool? hidden { get; set; } = false;
        public GraphModel model { get; set; }
    }
}
