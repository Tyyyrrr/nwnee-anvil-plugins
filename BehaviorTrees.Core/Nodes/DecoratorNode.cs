namespace BehaviorTrees.Core.Nodes
{
    /// <summary>
    /// This node has exactly one child.
    /// </summary>
    public abstract class DecoratorNode : Node
    {
        protected readonly Node Child;
        public DecoratorNode(Node child) {Child=child;}
    }
}