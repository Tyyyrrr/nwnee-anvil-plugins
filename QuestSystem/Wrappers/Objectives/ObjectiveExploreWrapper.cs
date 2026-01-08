using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveExploreWrapper : ObjectiveWrapper<ObjectiveExplore>
    {
        public ObjectiveExploreWrapper(Objective objective) : base(objective) { }

        public override ObjectiveExplore Objective => base.Objective;

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