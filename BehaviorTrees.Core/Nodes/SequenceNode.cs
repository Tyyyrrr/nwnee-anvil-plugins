namespace BehaviorTrees.Core.Nodes
{
    /// <inheritdoc cref="CompositeNode"/>
    /// <remarks>
    /// Returns <see cref="NodeStatus.Success"/> only when every child succeeds
    /// </remarks>
    public sealed class SequenceNode : CompositeNode
    {
        public SequenceNode(params Node[] nodes) : base(nodes){}

        public override NodeStatus Evaluate(IBehaviorState data)
        {
            foreach(var child in Children)
            {
                var status = child.Evaluate(data);
                if(status != NodeStatus.Success)
                    return status;
            }
            return NodeStatus.Success;
        }
    }
}