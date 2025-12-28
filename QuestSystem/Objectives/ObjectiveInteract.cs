using System;
using Anvil.API;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveInteract : Objective
    {
        public enum InteractionType
        {
            PlaceableUse,
            ItemActivate,
            TriggerEnter,
            ObjectExamine
        }

        public InteractionType Interaction {get;set;}

        public string ResRef {get;set;} = string.Empty;
        public string Tag {get;set;} = string.Empty;

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