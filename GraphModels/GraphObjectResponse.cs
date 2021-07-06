using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class GraphObjectResponse
    {
        public GraphObject getGraphObjectByExternalId { get; set; }
        public GraphObject createGraphObject { get; set; }

    }
}
