using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkBase.Client.GraphModels;

namespace ThinkBase.Client
{
    public interface IClient
    {
        Task<KnowledgeState> CreateKnowledgeState(KnowledgeState ks);
        Task<GraphModel> FetchModel();
        Task<KnowledgeState> GetKnowledgeState(string subjectId);
    }
}
