using System;

namespace BehaviorTrees.Core.Nodes
{    
    /// <inheritdoc cref="LeafNode"/>
    /// <remarks>
    /// Always returns <see cref="NodeStatus.Success"/>
    /// </remarks>
    public sealed class ActionNode : LeafNode
    {
        private readonly Action<IBehaviorState> _action;
        public ActionNode(Action<IBehaviorState> action) { _action = action;}
        public override NodeStatus Evaluate(IBehaviorState data)
        {
            _action(data);
            return NodeStatus.Success;
        }
    }
}