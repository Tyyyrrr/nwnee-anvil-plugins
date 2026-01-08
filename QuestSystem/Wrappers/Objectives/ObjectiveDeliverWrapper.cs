using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveDeliverWrapper : ObjectiveWrapper<ObjectiveDeliver>
    {
        public ObjectiveDeliverWrapper(Objective objective) : base(objective) { }
        public override ObjectiveDeliver Objective => base.Objective;

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