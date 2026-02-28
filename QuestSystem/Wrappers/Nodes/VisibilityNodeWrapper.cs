using Anvil.API;
using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class VisibilityNodeWrapper : NodeWrapper<VisibilityNode>
    {
        public VisibilityNodeWrapper(VisibilityNode node) : base(node){}

        protected override void ProtectedDispose()
        {
            // noting to dispose atm
        }

        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            nextId = Node.NextID;

            QuestManager.Instance.CombineVisibility(player, Quest!.Tag, ID);

            return true;
        }
    }
}