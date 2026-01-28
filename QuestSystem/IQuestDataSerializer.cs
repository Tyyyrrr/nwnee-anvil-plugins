using System.IO;
using System.Text.Json;
using QuestSystem.Nodes;

namespace QuestSystem
{
    public interface IQuestDataSerializer
    {
        public static readonly JsonSerializerOptions Options;

        static IQuestDataSerializer()
        {
            Options = new()
            {                
                PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Replace,
                WriteIndented = false,
                AllowTrailingCommas = false,
                MaxDepth = 6,
                IncludeFields = false,
            };

            var cleanOptions = new JsonSerializerOptions(Options);

            Options.Converters.Add(new NodeConverter(cleanOptions));
        }

        public void SerializeQuestToStream(Quest quest, Stream stream);
        public void SerializeNodeToStream(NodeBase node, Stream stream);
        public Quest? DeserializeQuestFromStream(Stream stream);
        public NodeBase? DeserializeNodeFromStream(Stream stream);
    }
}