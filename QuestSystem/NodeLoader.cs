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
            
            NLog.LogManager.GetCurrentClassLogger().Info("Loading node!");

            NodeWrapper? node = null;
            try
            {
                node = GetNodeImmediate(quest,nodeId) as NodeWrapper;
            }
            catch(UnknownNodeWrapException ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex.Message + "\nRawData:\n" + ex.RawNodeData);
                node?.Dispose();
                return null;
            }
            
            if(node != null)
                node.Quest = quest;

            return node;
        }


        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="UnknownNodeWrapException"></exception>
        private static INode? GetNodeImmediate(Quest quest, int id)
        {
            var pack = (quest.Pack as RuntimeQuestPack) ?? throw new InvalidOperationException("RuntimeQuestPack does not exist");

            var nodeBase = pack.GetNode(quest.Tag, id)
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