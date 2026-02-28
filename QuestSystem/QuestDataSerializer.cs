using System.IO;
using System.Text.Json;
using QuestSystem.Nodes;

namespace QuestSystem
{
    internal sealed class QuestDataSerializer : IQuestDataSerializer
    {
        public NodeBase? DeserializeNodeFromStream(Stream stream) => JsonSerializer.Deserialize<NodeBase>(stream, IQuestDataSerializer.Options);
        public Quest? DeserializeQuestFromStream(Stream stream) => JsonSerializer.Deserialize<Quest>(stream, IQuestDataSerializer.Options);
        public void SerializeQuestToStream(Quest quest, Stream stream) => JsonSerializer.Serialize(stream, quest, IQuestDataSerializer.Options);
        public void SerializeNodeToStream(NodeBase node, Stream stream) => JsonSerializer.Serialize(stream, node, IQuestDataSerializer.Options);
    }
}