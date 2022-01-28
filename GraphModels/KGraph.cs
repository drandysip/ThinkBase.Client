using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class KGraph
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DarlTime? fixedTime { get; set; }
        public string userId { get; set; } = string.Empty;
        public bool Shared { get; set; } = false;
        public string OwnerId { get; set; } = string.Empty;
        public bool? ReadOnly { get; set; } = false;
        public string InitialText { get; set; } = string.Empty;
        public bool? hidden { get; set; } = false;
        public GraphModel? model { get; set; }
    }
}
