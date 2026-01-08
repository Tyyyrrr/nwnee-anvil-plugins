using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveSpellcastWrapper : ObjectiveWrapper<ObjectiveSpellcast>
    {
        public ObjectiveSpellcastWrapper(Objective objective) : base(objective) { }

        public override ObjectiveSpellcast Objective => base.Objective;


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