using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveDeliverWrapper : ObjectiveWrapper<ObjectiveDeliver>
    {
        public ObjectiveDeliverWrapper(ObjectiveDeliver objective) : base(objective) { }
        protected override ObjectiveDeliver Objective => base.Objective;

        protected override void Subscribe()
        {
            if(string.IsNullOrEmpty(Objective.ItemResRef) && string.IsNullOrEmpty(Objective.ItemTag))
            {
                _log.Error("ObjectiveObtain needs ResRef, Tag or both, but none was provided");
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