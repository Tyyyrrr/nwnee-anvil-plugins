using System;
using System.Linq;
using System.Threading;
using Anvil.API;
using NLog;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestStageWrapper : BaseWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly QuestStage _questStage;
        public int ID => _questStage.ID;
        public bool ShowInJournal => _questStage.ShowInJournal;

        public string? GetStageJournalEntry(NwPlayer player)
        {
            if(!ShowInJournal) return string.Empty;

                //todo: add regexp for macros in the text

            return _questStage.JournalEntry;
        }
        public string? GetObjectivesJournalEntry(NwPlayer player)
        {
            if(!ShowInJournal || _objectives.Length == 0) return string.Empty;

            string text = string.Empty;

            foreach(var obj in _objectives.Where(o=>o.ShowInJournal))
                text += obj.GetJournalText(player);

            return text;
        }

        private readonly ObjectiveWrapper[] _objectives;

        public readonly QuestStageRewardWrapper Reward;

        public event Action<QuestStageWrapper, NwPlayer, int>? AutoCompleted;
        public event Action<QuestStageWrapper, NwPlayer>? QuestAutoCompleted;
        public event Action<QuestStageWrapper, NwPlayer>? Updated;

        public QuestStageWrapper(QuestStage questStage)
        {
            _questStage = questStage;
            Reward = new(_questStage.Reward);
            _objectives = new ObjectiveWrapper[questStage.Objectives.Length];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = questStage.Objectives[i].Wrap();
                _objectives[i].Updated += OnObjectiveUpdated;
            }
        }

        public bool ManualComplete(NwPlayer player)
        {
            if(_objectives.Length > 0)
            {
                _log.Error("Stage with objectives can not be manually completed.");
                return false;
            }

            Reward.GiveReward(player);

            return true;
        }
        
        void OnObjectiveUpdated(ObjectiveWrapper wrapper, NwPlayer player)
        {
            if (wrapper.IsCompleted(player))
            {
                var nextId = wrapper.Objective.NextStageID;

                wrapper.Reward.GiveReward(player);

                switch (nextId)
                {
                    case -2: 
                        QuestAutoCompleted?.Invoke(this, player);
                    break;

                    case -1:
                        if(_objectives.Any(o=>o.Objective.NextStageID == -1 && !o.IsCompleted(player)))
                            Updated?.Invoke(this, player);
                        else
                        {
                            Reward.GiveReward(player);
                            AutoCompleted?.Invoke(this, player, _questStage.NextStageID);
                        }
                    break;

                    default:
                        AutoCompleted?.Invoke(this, player, nextId);
                    break;
                }
            }

            else Updated?.Invoke(this, player);
        }

        public int TrackedPlayersCount {get;private set;} = 0;

        public void TrackProgress(NwPlayer player)
        {
            TrackedPlayersCount++;
            foreach (var objective in _objectives)
                objective.StartTrackingProgress(player);
        }

        public void StopTracking(NwPlayer player)
        {
            TrackedPlayersCount--;
            foreach (var objective in _objectives)
                objective.StopTrackingProgress(player);
        }

        public void StopTracking()
        {
            TrackedPlayersCount=0;
            foreach(var objective in _objectives)
                objective.StopTrackingProgress();
        }

        public override void Dispose()
        {
            base.Dispose();

            TrackedPlayersCount=0;
            foreach(var objective in _objectives)
                objective.Dispose();
        }
    }
}