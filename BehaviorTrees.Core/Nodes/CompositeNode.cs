using System.Collections.Generic;

namespace BehaviorTrees.Core.Nodes
{
    /// <<summary>
    /// This node has multiple children.
    /// </summary>>
    public abstract class CompositeNode : Node
    {
        protected readonly IReadOnlyList<Node> Children;
        public CompositeNode(params Node[] children) { Children = children; }
    }
}