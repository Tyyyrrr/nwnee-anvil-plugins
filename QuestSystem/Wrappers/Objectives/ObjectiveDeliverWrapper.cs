using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveDeliverWrapper : ObjectiveWrapper<ObjectiveDeliver>
    {
        public ObjectiveDeliverWrapper(Objective objective) : base(objective) { }
        public override ObjectiveDeliver Objective => base.Objective;

        protected override void Subscribe()
        {
            throw new System.NotImplementedException();
        }

        protected override void Unsubscribe()
        {
            throw new System.NotImplementedException();
        }
    }
}