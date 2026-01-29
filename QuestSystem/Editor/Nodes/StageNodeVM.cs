using QuestEditor.Explorer;
using QuestSystem.Nodes;

namespace QuestEditor.Nodes
{
    public sealed class StageNodeVM(StageNode node, QuestVM quest) : NodeVM(node, quest)
    {
        protected override StageNode Node => (StageNode)base.Node;
    }
}