using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using NLog;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestStageWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly QuestStage _questStage;

        private readonly ObjectiveWrapper[] _objectives;

        public QuestWrapper? Quest { get; set; } = null;
        public readonly QuestStageRewardWrapper Reward;

        public int ID => _questStage.ID;

        public QuestStageWrapper(QuestStage questStage)
        {
            _questStage = questStage;
            Reward = new(_questStage.Reward);
            _objectives = new ObjectiveWrapper[questStage.Objectives.Length];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = questStage.Objectives[i].Wrap();
                _objectives[i].QuestStage = this;
            }
        }

        private bool AssertQuestValid()
        {
            if (Quest == null)
            {
                _log.Error("No parent Quest");
                foreach (var objective in _objectives)
                    objective.StopTrackingProgress();
                return false;
            }
            return true;
        }
        internal void TrackProgress(NwPlayer player)
        {
            if (!AssertQuestValid()) return;

            if (!player.IsValid)
            {
                _log.Error("Player invalidated");
                return;
            }

            _log.Warn($"Tracking progress for player {player.PlayerName} on {Quest?.Tag ?? " -- NO QUEST -- "}/{ID}");

            foreach (var objective in _objectives)
            {
                objective.StartTrackingProgress(player);
            }

        }

        internal void StopTracking(NwPlayer player)
        {
            foreach (var objective in _objectives)
            {
                objective.StopTrackingProgress(player);
            }
        }

        internal bool IsTracking(NwPlayer player) => _objectives.Any(o => o.IsTracking(player));
        internal bool IsActive => _objectives.Any(o => o.IsActive);


        private readonly HashSet<NwPlayer> _scheduledJournalUpdates = new();
        internal async void ScheduleJournalUpdate(NwPlayer player)
        {
            if (!_scheduledJournalUpdates.Add(player)) return;

            _log.Warn("Journal update scheduled!");

            await NwTask.Delay(TimeSpan.FromSeconds(0.6));

            if (!_scheduledJournalUpdates.Remove(player)) return;

            else if (player.IsValid && IsTracking(player)) UpdateJournal(player);
        }

        private void UpdateJournal(NwPlayer player)
        {
            if (!AssertQuestValid()) return;

            _log.Warn("Updating journal...");

            string[] parts = new string[1 + _objectives.Length];
            parts[0] = _questStage.JournalEntry;
            bool allCompleted = true;
            for (int i = 1; i < _objectives.Length + 1; i++)
            {
                var objective = _objectives[i - 1];
                if (!objective.IsCompleted(player)) allCompleted = false;
                parts[i] = objective.GetJournalText(player);
            }
            string str = string.Join("\n", parts);

            var entry = player.GetJournalEntry(Quest!.Tag);
            if (entry == null)
            {
                entry = new();
                entry.QuestTag = Quest.Tag;
                entry.Name = Quest.Name;
                entry.QuestDisplayed = true;
                entry.QuestCompleted = allCompleted;
                entry.Text = str;
                entry.Updated = true;

                player.AddJournalQuestEntry(Quest!.Tag, 0, false, true); // test
            }
            else
            {
                entry.Text = str;
                entry.QuestCompleted = allCompleted;
                entry.Updated = true;
                player.AddJournalQuestEntry(entry.QuestTag, 0, false, true); // test
            }
        }
    }
}