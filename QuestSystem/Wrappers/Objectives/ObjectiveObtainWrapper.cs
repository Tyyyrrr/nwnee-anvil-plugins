using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveObtainWrapper : ObjectiveWrapper<ObjectiveObtain>
    {
        public ObjectiveObtainWrapper(Objective objective) : base(objective) { }

        public override ObjectiveObtain Objective => base.Objective;

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