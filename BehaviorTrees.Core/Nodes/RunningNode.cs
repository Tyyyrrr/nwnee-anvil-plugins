namespace BehaviorTrees.Core.Nodes
{    
    /// <inheritdoc cref="LeafNode"/>
    /// <remarks>
    /// Always returns <see cref="NodeStatus.Running"/>
    /// </remarks>
    public sealed class RunningNode : LeafNode
    {
        public override NodeStatus Evaluate(IBehaviorState data)
        {
            return NodeStatus.Running;
        }
    }
}