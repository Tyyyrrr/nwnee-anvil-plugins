using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anvil.API;
using QuestSystem.Objectives;

namespace QuestSystem
{
    public sealed class QuestStage
    {
        internal Quest? Quest;

        public int ID {get;set;}
        public int NextStageID {get;set;}
        public string JournalEntry {get;set;} = string.Empty;
        public QuestStageReward Reward {get;set;} = new();
        public Objective[] Objectives {get;set;} = Array.Empty<Objective>();

        private readonly HashSet<NwPlayer> _trackedCreatures = new();

        internal void TrackProgress(NwPlayer player)
        {
            if(!player.IsValid)
            {
                Quest.ClearPlayer(player);
                return;
            }

            if(!_trackedCreatures.Add(player)) return;

            foreach(var objective in Objectives)
            {
                objective.StartTrackingProgress(player);
            }

        }

        internal void StopTracking(NwPlayer player)
        {
            if(!_trackedCreatures.Remove(player)) return;

            if (!player.IsValid)
            {
                Quest.ClearPlayer(player);
                return;
            }

            foreach(var objective in Objectives)
            {
                objective.StopTrackingProgress(player);
            }
        }

        internal bool IsTracking(NwPlayer player) => Objectives.Any(o => o.IsTracking(player));
        internal bool IsActive => Objectives.Any(o => o.IsActive);


        private readonly HashSet<NwPlayer> _scheduledJournalUpdates = new();
        internal async void ScheduleJournalUpdate(NwPlayer player)
        {
            if(!_scheduledJournalUpdates.Add(player)) return;

            await NwTask.Delay(TimeSpan.FromSeconds(0.6));

            if(!_scheduledJournalUpdates.Remove(player)) return;

            if(!player.IsValid) Quest.ClearPlayer(player);
            else if(IsTracking(player)) UpdateJournal(player);
        }

        private void UpdateJournal(NwPlayer player)
        {
            var quest = Quest ?? throw new InvalidOperationException("No parent Quest");

            string[] parts = new string[1 + Objectives.Length];
            parts[0] = JournalEntry;
            bool allCompleted = true;
            for(int i = 1; i < Objectives.Length + 1; i++)
            {
                var objective = Objectives[i-1];
                if(!objective.IsCompleted(player)) allCompleted = false;
                parts[i] = objective.GetJournalText(player);
            }
            string str = string.Join("\n", parts);

            var entry = player.GetJournalEntry(quest.Tag);
            if(entry == null)
            {
                entry = new();
                entry.QuestTag = quest.Tag;
                entry.Name = quest.Name;
                entry.QuestDisplayed = true;
                entry.QuestCompleted = allCompleted;
                entry.Text = str;
                entry.Updated = true;

                player.AddCustomJournalEntry(entry);
            }
            else
            {
                entry.Text = str;
                entry.QuestCompleted = allCompleted;
                entry.Updated = true;
                player.AddCustomJournalEntry(entry, true);
            }
        }
    }
}