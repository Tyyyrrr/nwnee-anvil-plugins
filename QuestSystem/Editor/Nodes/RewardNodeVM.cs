using QuestEditor.Explorer;
using QuestSystem.Nodes;

namespace QuestEditor.Nodes
{
    public sealed class RewardNodeVM(RewardNode node, QuestVM quest) : SingleOutputNodeVM(node, quest)
    {
        protected override RewardNode Node => (RewardNode)base.Node;

    }
}