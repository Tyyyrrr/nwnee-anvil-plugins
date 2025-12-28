using System;
using Anvil.API;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveExplore : Objective
    {
        public string AreaTag {get;set;} = string.Empty;
        public int AreaExplorePercentage {get;set;}

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