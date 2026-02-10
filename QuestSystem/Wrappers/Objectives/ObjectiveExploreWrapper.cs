using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveExploreWrapper : ObjectiveWrapper<ObjectiveExplore>
    {
        public ObjectiveExploreWrapper(ObjectiveExplore objective) : base(objective) { }

        protected override ObjectiveExplore Objective => base.Objective;

        protected override void Subscribe()
        {
            if(Objective.AreaTags.Length == 0)
            {
                _log.Error("ObjectiveExplore needs at least one area tag, but none was provided");
                return;
            }
            
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