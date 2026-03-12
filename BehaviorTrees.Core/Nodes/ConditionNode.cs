using System;

namespace BehaviorTrees.Core.Nodes
{
    /// <inheritdoc cref="LeafNode"/>
    /// <remarks>
    /// Returns either <see cref="NodeStatus.Failure"/> or <see cref="NodeStatus.Success"/>
    /// </remarks>
    public sealed class ConditionNode : LeafNode
    {
        private readonly Func<IBehaviorState, bool> _condition;
        public ConditionNode(Func<IBehaviorState, bool> condition) {_condition = condition;}
        public override NodeStatus Evaluate(IBehaviorState data)
        {
            return _condition(data) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}