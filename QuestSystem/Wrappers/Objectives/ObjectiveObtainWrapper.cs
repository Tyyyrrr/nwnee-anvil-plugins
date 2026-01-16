using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveObtainWrapper : ObjectiveWrapper<ObjectiveObtain>
    {
        public ObjectiveObtainWrapper(ObjectiveObtain objective) : base(objective) { }

        protected override ObjectiveObtain Objective => base.Objective;

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