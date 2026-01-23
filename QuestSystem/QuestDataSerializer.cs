using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuestSystem.Nodes;

namespace QuestSystem
{
    internal sealed class QuestDataSerializer : IQuestDataSerializer
    {
        public Quest? DeserializeQuestFromStream(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public NodeBase? DeserializeNodeFromStream(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public Task<NodeBase?> DeserializeNodeFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<Quest?> DeserializeQuestFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task SerializeToStreamAsync(Quest quest, Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public Task SerializeToStreamAsync(NodeBase node, Stream stream)
        {
            throw new System.NotImplementedException();
        }
    }
}