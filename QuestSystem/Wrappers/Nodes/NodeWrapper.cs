using System;
using Anvil.API;
using QuestSystem.Graph;
using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    internal abstract class NodeWrapper : WrapperBase, INode
    {
        private readonly NodeBase _node;
        public virtual NodeBase Node => _node;
        public NodeWrapper(NodeBase node) { _node = node; }

        public int ID => _node.ID;
        public virtual int NextID => _node.NextID;
        public bool Rollback => _node.Rollback;

        public virtual bool IsRoot {get;} = false;

        public event Action<INode, NwPlayer>? ShouldEvaluate;
        protected void RaiseShouldEvaluate(NwPlayer player) => ShouldEvaluate?.Invoke(this,player);

        protected abstract bool ProtectedEvaluate(NwPlayer player, out int nextId);

        public bool Evaluate(NwPlayer player, out int nextId)
        {            
            ThrowIfDisposed();

            if (ProtectedEvaluate(player, out nextId))
            {
                return true;
            }
            else return false;
        }

        public virtual void Reset(NwPlayer player){}

        public virtual void Enter(NwPlayer player){}
    }
    /// <summary>
    /// Generic warpper for attaching custom behaviors to plain-data quest Nodes.
    /// </summary>
    /// <typeparam name="T">Type of underlying domain data model</typeparam>
    internal abstract class NodeWrapper<T> : NodeWrapper, INode where T : NodeBase
    {
        public override T Node => (T)base.Node;
        public NodeWrapper(T node)  : base(node){}

    }
}