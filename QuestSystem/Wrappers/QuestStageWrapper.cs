using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;
using NLog;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestStageWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly QuestStage _questStage;

        public QuestWrapper? Quest { get; set; } = null;
        private readonly ObjectiveWrapper[] _objectives;
        public readonly QuestStageRewardWrapper Reward;

        private readonly HashSet<NwPlayer> _trackedPlayers = new();

        public int ID => _questStage.ID;

        public event Action<QuestStageWrapper, NwPlayer, int>? Completed;

        public QuestStageWrapper(QuestStage questStage)
        {
            _questStage = questStage;
            Reward = new(_questStage.Reward);
            _objectives = new ObjectiveWrapper[questStage.Objectives.Length];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = questStage.Objectives[i].Wrap();
                _objectives[i].Completed += OnObjectiveCompleted;
            }
        }


        private void ThrowIfQuestIsNull() {if(Quest==null) throw new InvalidOperationException("Quest is null.");}

        public bool Complete(NwPlayer player)
        {
            if(!IsTracking(player)) 
                return false;

            if(_objectives.Any(o=>o.Objective.NextStageID >= 0 && o.Objective.NextStageID != _questStage.NextStageID))
            {
                _log.Error("Stage with objectives can not be manually completed.");
                return false;
            }

            StopTracking(player);
            Reward.GrantReward(player);
            Completed?.Invoke(this, player, _questStage.NextStageID);
            return true;
        }
        
        void OnObjectiveCompleted(ObjectiveWrapper wrapper, NwPlayer player)
        {
            ThrowIfQuestIsNull();

            int nextStageId = wrapper.Objective.NextStageID;

            QuestStageRewardWrapper reward;

            if(nextStageId >= 0 && nextStageId != ID)
            {
                reward = wrapper.Reward;
            }
            else if (_objectives.Any(o => !o.IsCompleted(player)))
            {
                wrapper.StopTrackingProgress(player);
                _ = ScheduleJournalUpdate(player);
                return;
            }
            else
            {
                reward = this.Reward;
                nextStageId = this._questStage.NextStageID;
            }

            StopTracking(player);
            reward.GrantReward(player);
            Completed?.Invoke(this, player, nextStageId);
        }

        internal void TrackProgress(NwPlayer player)
        {
            ThrowIfQuestIsNull();

            if (!player.IsValid)
            {
                _log.Error("Player invalidated");
                if(_trackedPlayers.Remove(player))
                    foreach(var obj in _objectives)
                        obj.StopTrackingProgress(player);
                return;
            }

            _log.Warn($"Tracking progress for player {player.PlayerName} on {Quest?.Tag ?? " -- NO QUEST -- "}/{ID}");

            if(_trackedPlayers.Add(player))
                foreach (var objective in _objectives)
                {
                    objective.StartTrackingProgress(player);
                }
        }

        internal void StopTracking(NwPlayer player)
        {
            if(_trackedPlayers.Remove(player))
                foreach (var objective in _objectives)
                {
                    objective.StopTrackingProgress(player);
                }
        }

        internal bool IsTracking(NwPlayer player) => _trackedPlayers.Contains(player);
        internal bool IsActive => _trackedPlayers.Count > 0;

        private readonly HashSet<NwPlayer> _scheduledJournalUpdates = new();
        internal async Task ScheduleJournalUpdate(NwPlayer player)
        {
            if (!_scheduledJournalUpdates.Add(player)) return;

            _log.Warn("Journal update scheduled!");

            await NwTask.Delay(TimeSpan.FromSeconds(0.6));
            await NwTask.SwitchToMainThread();

            if (!_scheduledJournalUpdates.Remove(player)) return;
            else if (player.IsValid && IsTracking(player)) UpdateJournal(player);
        }

        private void UpdateJournal(NwPlayer player)
        {
            ThrowIfQuestIsNull();

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