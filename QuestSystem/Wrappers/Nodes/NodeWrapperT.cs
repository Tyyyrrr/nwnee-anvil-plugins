using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Anvil.API;
using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    /// <summary>
    /// Generic warpper for attaching custom behaviors to plain-data quest Nodes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class NodeWrapper<T> : WrapperBase where T : NodeBase
    {
        public int ID => _node.ID;
        public int NextID => _node.NextID;

        /// <summary>
        /// Direct parent nodes from which this node originated.
        /// A parent node stays alive as long as this node exists.
        /// Only direct parents are stored — ancestors are kept alive indirectly.
        /// </summary>
        public HashSet<int> ParentNodes { get; } = new();

        /// <summary>
        /// Number of players currently on this node PLUS the number of child nodes
        /// that directly depend on this node (i.e., nodes that list this node as a parent).
        /// This node remains alive as long as RefCount > 0.
        /// </summary>
        public int RefCount { get; set; }

        private readonly T _node;
        protected T Node => _node;
        public NodeWrapper(T node) {_node = node; }

        public event Action<NodeWrapper<T>,NwPlayer,object>? Succeeded;
        public event Action<NodeWrapper<T>,NwPlayer>? Failed;

        public void Evaluate(NwPlayer player)
        {
            ThrowIfDisposed();

            if (Evaluate(player, out var result))
                Succeeded?.Invoke(this,player,result);
                
            else 
                Failed?.Invoke(this,player);
        }

        protected abstract bool Evaluate(NwPlayer player, [MaybeNullWhen(false)] [NotNullWhen(true)] out object? result);

    }
}