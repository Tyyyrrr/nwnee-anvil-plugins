using QuestSystem.Nodes;

namespace QuestEditor.Nodes
{
    public sealed class RewardNodeVM(RewardNode node) : NodeVM(node)
    {
        protected override RewardNode Node => (RewardNode)base.Node;
    }
}