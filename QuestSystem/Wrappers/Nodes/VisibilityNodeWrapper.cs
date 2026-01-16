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
            nextId = NextID;
            foreach(var kvp in Node.Objects)
            {
                // todo:
                // add personal visibility override on listed objects for this player
            }
            return true;
        }
    }
}