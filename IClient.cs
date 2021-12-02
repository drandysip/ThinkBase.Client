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
        Task<long> ClearAllKnowledgeStates();
        Task<KnowledgeState> CreateKnowledgeState(KnowledgeState ks);
        Task<List<KnowledgeState>> CreateKnowledgeStateBatched(List<KnowledgeState> ksl);
        Task<String> ExportNodaModel();
        Task<GraphModel> FetchModel();
        Task<KnowledgeState> GetKnowledgeState(string subjectId);
        Task SetConnectionPresence(KnowledgeState ks, string nodeName, string destName, string remoteSubjectId);
        Task SetDataValue(KnowledgeState ks, string nodeName, string attName, string value);
        Task SetObjectExistence(KnowledgeState ks, string nodeName, List<DarlTime> existence);
    }
}
