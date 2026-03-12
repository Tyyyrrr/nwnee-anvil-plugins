namespace BehaviorTrees.Core.Nodes
{
    public abstract class Node
    {
        public abstract NodeStatus Evaluate(IBehaviorState data);
    }
}