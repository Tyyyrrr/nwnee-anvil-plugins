using System;
using System.Collections.Generic;
using Anvil.API;
using QuestSystem.Objectives.Templates;

namespace QuestSystem
{
    public sealed class QuestStage
    {
        internal Quest? Quest;

        public int ID {get;set;}
        public int NextStageID {get;set;}
        public string JournalEntry {get;set;} = string.Empty;
        public QuestStageReward Reward {get;set;} = new();
        public ObjectiveTemplate[] Templates {get;set;} = Array.Empty<ObjectiveTemplate>();

        private readonly HashSet<NwCreature> _trackedCreatures = new();

        internal void TrackProgress(NwCreature pc)
        {
            if(!pc.IsValid)
            {
                Quest.ClearPC(pc);
                return;
            }

            if(!_trackedCreatures.Add(pc)) return;

            foreach(var template in Templates)
            {
                // template.StartTracking(pc);
            }

        }

        internal void StopTracking(NwCreature pc)
        {
            if(!_trackedCreatures.Remove(pc)) return;

            if (!pc.IsValid)
            {
                Quest.ClearPC(pc);
                return;
            }

            foreach(var template in Templates)
            {
                // template.StopTracking(pc);
            }
        }

        internal bool IsTracking(NwCreature pc) => _trackedCreatures.Contains(pc);

        internal bool IsActive => _trackedCreatures.Count > 0;
    }
}