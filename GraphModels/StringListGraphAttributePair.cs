using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class StringListGraphAttributePair
    {
        public string Name { get; set; }
        public List<GraphAttribute> Value { get; set; }
    }
}
