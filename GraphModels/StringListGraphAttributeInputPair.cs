using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class StringListGraphAttributeInputPair
    {
        public string name { get; set; } = string.Empty;
        public List<GraphAttributeInput> value { get; set; } = new List<GraphAttributeInput> { };
    }
}
