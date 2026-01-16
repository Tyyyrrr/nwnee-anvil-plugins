using System;
using System.IO;

using QuestSystem.Graph;
using QuestSystem.Nodes;
using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem
{
    internal sealed class NodeLoader : INodeLoader
    {
        INode? INodeLoader.LoadNode(Quest quest, int nodeId) 
        {
            var node = GetNodeImmediate(quest,nodeId) as NodeWrapper;
            if(node != null)
                node.Quest = quest;

            return node;
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