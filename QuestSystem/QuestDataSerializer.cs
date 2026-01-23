using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuestSystem.Nodes;

namespace QuestSystem
{
    internal sealed class QuestDataSerializer : IQuestDataSerializer
    {
        public Quest? DeserializeQuestFromStream(Stream stream)
        {
            try
            {
                return JsonSerializer.Deserialize<Quest>(stream, IQuestDataSerializer.Options);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public NodeBase? DeserializeNodeFromStream(Stream stream)
        {
            try
            {
                return JsonSerializer.Deserialize<NodeBase>(stream, IQuestDataSerializer.Options);
            }
            catch(Exception)
            {
                return null;
            }
        }


        public async Task SerializeToStreamAsync(Quest quest, Stream stream)
        {
            try
            {
                await JsonSerializer.SerializeAsync(stream, quest, IQuestDataSerializer.Options);
            }
            catch(Exception){}
        }

        public async Task SerializeToStreamAsync(NodeBase node, Stream stream)
        {            
            try
            {
                await JsonSerializer.SerializeAsync(stream, node, IQuestDataSerializer.Options);
            }
            catch(Exception){}
        }


        public async Task<Quest?> DeserializeQuestFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {            
            try
            {
                return await JsonSerializer.DeserializeAsync<Quest>(stream, IQuestDataSerializer.Options, cancellationToken);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public async Task<NodeBase?> DeserializeNodeFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<NodeBase>(stream, IQuestDataSerializer.Options, cancellationToken);
            }
            catch(Exception)
            {
                return null;
            }
        }

    }
}