using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class StringListGraphAttributeInputPair
    {
        public string Name { get; set; }
        public List<GraphAttributeInput> Value { get; set; }
    }
}
