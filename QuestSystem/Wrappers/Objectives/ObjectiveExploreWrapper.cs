using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveExploreWrapper : ObjectiveWrapper<ObjectiveExplore>
    {
        public ObjectiveExploreWrapper(ObjectiveExplore objective) : base(objective) { }

        protected override ObjectiveExplore Objective => base.Objective;

        protected override void Subscribe()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Fake subscribe...");
            return;
        }

        protected override void Unsubscribe()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Fake unsubscribe...");
            return;
        }
    }
}