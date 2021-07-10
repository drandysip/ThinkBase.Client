using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkBase.Client.GraphModels;

namespace ThinkBase.Client
{
    public static class KSHelper
    {
        /// <summary>
        /// Modifies a KS value provided by a GetKnowledgeStateBlank function
        /// </summary>
        /// <param name="ks">The Knowledge state to modify</param>
        /// <param name="name">The name of the attribute</param>
        /// <param name="value">The value to set</param>
        /// <returns></returns>
        public static void SetDataValue(this KnowledgeState ks, string name, string value)
        {
            bool found = false;
            foreach( var l in ks.data.Values.ToList()[0])
            {
                if(l.name == name)
                {
                    l.value = value;
                    found = true;
                    break;
                }
            }
            if(!found)
            {
                throw new ThinkBaseException($"Attribute {name} not found. Schema change?");
            }
        }

 
        public static void SetConnectionPresence(this KnowledgeState ks, string destName, string remoteSubjectId)
        {
            bool found = false;
            foreach (var p in ks.data.Keys)
            {
                foreach(var q in ks.data[p])
                {
                    if(q.inferred && q.type == GraphAttribute.DataType.connection && q.id == destName)
                    {
                        q.value = remoteSubjectId;
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                throw new ThinkBaseException($"Connection {destName} not found. Schema change?");
            }
        }

    }
}
