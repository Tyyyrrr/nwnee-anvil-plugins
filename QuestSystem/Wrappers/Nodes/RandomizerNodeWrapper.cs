using Anvil.API;
using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class RandomizerNodeWrapper : NodeWrapper<RandomizerNode>
    {
        public RandomizerNodeWrapper(RandomizerNode node) : base(node){}

        public override bool IsRoot => false;

        protected override void ProtectedDispose()
        {
            //nothing to dispose
        }

        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            nextId = Node.WeightedRandom();
            return true;
        }
    }
}