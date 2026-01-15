using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using QuestSystem.Graph;
using QuestSystem.Nodes;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    internal sealed class NodeLoader : INodeLoader
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        
        private readonly Dictionary<string, Quest> _quests;

        public NodeLoader(Dictionary<string,Quest> quests)
        {
            _quests = quests;
        }

        INode? INodeLoader.LoadNode(string questTag, int nodeId)
        {
            if(!_quests.TryGetValue(questTag, out var quest))
                return null;
            
            return GetNodeImmediate(quest,nodeId);
        }

        private static INode? GetNodeImmediate(Quest quest, int id)
        {
            var pack = quest.Pack ?? throw new InvalidOperationException("QuestPack does not exist");

            var entry = pack.GetEntry($"{quest.Tag}/{id}");
            if(entry == null) return null;

            using var stream = entry.Open();
            var nodeBase = QuestSerializer.Deserialize<NodeBase>(stream)
                ?? throw new InvalidDataException($"Node {id} is missing or invalid.");

            if (nodeBase is not IWrappable wrappable)
                throw new InvalidDataException($"Node {id} does not implement IWrappable.");

            var wrapped = wrappable.Wrap();
            if (wrapped is not INode node)
                throw new InvalidDataException($"Wrapped node {id} is not an INode.");

            return node;
        }
    }
}