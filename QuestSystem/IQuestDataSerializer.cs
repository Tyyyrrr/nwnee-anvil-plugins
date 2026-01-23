using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

            Options.Converters.Add(new NodeConverter());
        }

        public Task SerializeToStreamAsync(Quest quest, Stream stream);
        public Task SerializeToStreamAsync(NodeBase node, Stream stream);
        public Task<Quest?> DeserializeQuestFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);
        public Task<NodeBase?> DeserializeNodeFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}