using System;
using Anvil.API;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveDeliver : Objective
    {
        public string ItemResRef {get;set;} = string.Empty;
        public string ItemTag {get;set;} = string.Empty;
        public int RequiredAmount {get;set;}
        public bool AllowPartialDelivery {get;set;}
        public bool DestroyItemsOnDelivery {get;set;}

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