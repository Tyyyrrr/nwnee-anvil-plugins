using System;
using System.Collections.Generic;

namespace QuestSystem.Graph
{
    internal sealed partial class QuestGraph
    {
        /// <summary>
        /// Responsible for managing node lifetimes.
        /// </summary>
        private sealed class Storage : IDisposable
        {
            private sealed class NodeEntry
            {
                public readonly INode Node;
                public int RefCount{get;set;} = 0;
                public NodeEntry(INode node) {Node = node;}
            }

            private readonly Dictionary<int, NodeEntry> _nodes = new();
            public int Count => _nodes.Count;
            private readonly INodeLoader _nodeLoader;

            private readonly Quest _quest;

            public Storage(Quest quest, INodeLoader nodeLoader)
            {
                _quest = quest;
                _nodeLoader = nodeLoader;
            }

            #region API
            public void NodeIncrement(int id)
            {
                if(_nodes.TryGetValue(id, out var node))
                {
                    node.RefCount++;
                }
                else throw new InvalidOperationException($"Node is missing. QuestTag: {_quest.Tag}, Node: {id}");
            }
            public void NodeDecrement(int id)
            {
                if(_nodes.TryGetValue(id, out var node))
                {
                    node.RefCount--;
                    if(node.RefCount == 0)
                        RemoveNode(node.Node);
                    else if(node.RefCount<0)
                        throw new InvalidOperationException($"RefCount underflow\nQuestTag: {_quest.Tag}, Node: {id}");
                }
                else throw new InvalidOperationException($"Node is missing. QuestTag: {_quest.Tag}, Node: {id}");
            }

            public INode? GetOrCreateNode(PlayerCursor cursor)
            {
                if(!cursor.IsOnGraph) return null;

                if (_nodes.TryGetValue(cursor.Node, out var existing))
                    return existing.Node;

                if (!cursor.IsAtRoot && !_nodes.TryGetValue(cursor.Root, out _))
                    throw new InvalidOperationException($"Parent node {cursor.Root} must be loaded before adding child {cursor.Node}. (Quest: {_quest.Tag})");
                
                var node = _nodeLoader.LoadNode(_quest, cursor.Node);
                if (node == null)
                {
                    _log.Error($"Node loader failed to load node {cursor.Node} of quest \'{_quest.Tag}\'");
                    return null;
                }

                AddNode(node);

                return node;
            }

            public INode? this[int id] => _nodes.TryGetValue(id, out var entry) ? entry.Node : null;
            
            #endregion

            private void AddNode(INode node)
            {
                if(_nodes.TryGetValue(node.ID, out var existing))
                {
                    if(existing.Node != node) 
                        throw new InvalidOperationException("Tried to add a node to the graph, but another node with the same ID already exists.");
                    else
                        throw new InvalidOperationException("Tried to add the same node to the graph twice.");
                }

                _nodes.Add(node.ID, new(node));
            }

            private void RemoveNode(INode node)
            {
                if(_nodes.TryGetValue(node.ID, out var existing))
                {
                    if(existing.Node != node)
                        throw new InvalidOperationException("Tried to remove a node from the graph, but the node under the same ID key is a different object");
                }
                else throw new InvalidOperationException("Tried to remove a node which does not exist in this graph.");


                _nodes.Remove(node.ID);
                node.Dispose();
            }

            /// <summary>                
            /// Every player should alerady Exit the graph BEFORE disposal of the storage component.
            /// Every memory leak will be printed as a warning.
            /// Every negative RefCount will be printed as an error.
            /// </summary>
            public void Dispose()
            {
                foreach(var node in _nodes.Values)
                {
                    var str = $"Node {node.Node.ID} of quest \'{_quest.Tag}\' RefCount:{node.RefCount}{(node.RefCount == 0 ? " (leak)" : "")}";
                    if(node.RefCount>=0) 
                        _log.Warn(str);
                    else _log.Error(str);
                    
                    node.Node.Dispose();
                }
                _nodes.Clear();
            }
        }
    }
}