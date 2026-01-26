using QuestSystem.Nodes;

namespace QuestEditor.Nodes
{
    public sealed class StageNodeVM(StageNode node) : NodeVM(node)
    {
        protected override StageNode Node => (StageNode)base.Node;
    }
}