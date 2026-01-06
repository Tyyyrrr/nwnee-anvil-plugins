using System;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveSpellcast : Objective
    {
        public string TargetResRef {get;set;} = string.Empty;
        public string TargetTag {get;set;} = string.Empty;
        public int SpellID {get;set;} = -1;

        protected internal override void Subscribe()
        {
            throw new NotImplementedException();
        }

        protected internal override void Unsubscribe()
        {
            throw new NotImplementedException();
        }

        internal override IObjectiveProgress CreateProgress()
        {
            throw new NotImplementedException();
        }
    }
}