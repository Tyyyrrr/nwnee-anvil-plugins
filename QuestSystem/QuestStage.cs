using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;
using NLog;
using QuestSystem.Objectives;

namespace QuestSystem
{
    public sealed class QuestStage
    {

        public static string? Serialize(QuestStage questStage) => QuestSerializer.Serialize(questStage);
        public static QuestStage? Deserialize(string json) => QuestSerializer.Deserialize<QuestStage>(json);

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        internal Quest? Quest;

        public int ID {get;set;}
        public int NextStageID {get;set;}
        public string JournalEntry {get;set;} = string.Empty;
        public bool ShowInJournal {get;set;} = true;
        public QuestStageReward Reward {get;set;} = new();
        public Objective[] Objectives {get;set;} = Array.Empty<Objective>();

        private bool AssertQuestValid()
        {
            if(Quest == null)
            {
                _log.Error("No parent Quest");
                foreach(var objective in Objectives)
                    objective.StopTrackingProgress();
                return false;
            }
            return true;
        }
        internal void TrackProgress(NwPlayer player)
        {
            if(!AssertQuestValid()) return;

            if(!player.IsValid)
            {
                _log.Error("Player invalidated");
                Quest.ClearPlayer(player);
                return;
            }

            _log.Warn($"Tracking progress for player {player.PlayerName} on {Quest!.Tag}/{ID}");

            foreach(var objective in Objectives)
            {
                objective.StartTrackingProgress(player);
            }

        }

        internal void StopTracking(NwPlayer player)
        {
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

            _log.Warn("Journal update scheduled!");

            await NwTask.Delay(TimeSpan.FromSeconds(0.6));

            if(!_scheduledJournalUpdates.Remove(player)) return;

            if(!player.IsValid) Quest.ClearPlayer(player);
            else if(IsTracking(player)) UpdateJournal(player);
        }

        private void UpdateJournal(NwPlayer player)
        {
            if(!AssertQuestValid()) return;

            
            _log.Warn("Updating journal...");

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

            var entry = player.GetJournalEntry(Quest!.Tag);
            if(entry == null)
            {
                entry = new();
                entry.QuestTag = Quest.Tag;
                entry.Name = Quest.Name;
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