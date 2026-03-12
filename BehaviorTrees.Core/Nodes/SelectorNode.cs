namespace BehaviorTrees.Core.Nodes
{
    /// <inheritdoc cref="CompositeNode"/>
    /// <remarks>
    /// Returns the status of the first child that did not fail.
    /// </remarks>
    public sealed class SelectorNode : CompositeNode
    {
        public SelectorNode(params Node[] nodes) : base(nodes){}

        public override NodeStatus Evaluate(IBehaviorState data)
        {
            foreach(var child in Children)
            {
                var status = child.Evaluate(data);
                if(status != NodeStatus.Failure)
                    return status;
            }
            return NodeStatus.Failure;
        }
    }
}