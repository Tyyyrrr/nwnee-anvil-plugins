using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveSpellcastWrapper : ObjectiveWrapper<ObjectiveSpellcast>
    {
        public ObjectiveSpellcastWrapper(ObjectiveSpellcast objective) : base(objective) { }

        protected override ObjectiveSpellcast Objective => base.Objective;

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