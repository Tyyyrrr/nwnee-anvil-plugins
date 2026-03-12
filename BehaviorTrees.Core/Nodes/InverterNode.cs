namespace BehaviorTrees.Core.Nodes
{
    /// <inheritdoc cref="DecoratorNode"/>
    /// <remarks>
    /// Replaces <see cref="NodeStatus.Success"/> with <see cref="NodeStatus.Failure"/> or vice versa. Has no effect if child status is <see cref="NodeStatus.Running"/>
    /// </remarks>
    public sealed class InverterNode : DecoratorNode
    {
        public InverterNode(Node child) : base(child){}
        public override NodeStatus Evaluate(IBehaviorState data)
        {
            return Child.Evaluate(data) switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => NodeStatus.Running
            };
        }
    }
}