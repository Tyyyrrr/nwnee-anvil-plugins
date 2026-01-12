using System;
using System.Collections.Generic;
using Anvil.API;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestWrapper : BaseWrapper
    {
        private readonly Quest _quest;

        public string Tag => _quest.Tag;
        public string Name => _quest.Name;
        public int RegisteredStages {get;set;} = 0;

        public QuestWrapper(Quest quest)
        {
            _quest = quest;
        }

        private readonly Dictionary<int, QuestStageWrapper> _stages = new();

        /// <returns>False, if stage with the same ID is already registered</returns>
        public bool RegisterStage(QuestStageWrapper stage)
        {
            if(_stages.TryAdd(stage.ID, stage))
            {
                stage.AutoCompleted += OnStageAutoCompleted;
                stage.QuestAutoCompleted += OnQuestAutoCompleted;
                stage.Updated += OnStageUpdated;
                RegisteredStages++;
                return true;
            }
            return false;
        }

        /// <returns>False, if the stage with this ID was not registered by the quest, or is a different object</returns>
        public bool UnregisterStage(QuestStageWrapper stage)
        {
            if(_stages.TryGetValue(stage.ID, out var existing) && existing != stage) 
                return false;

            if (_stages.Remove(stage.ID))
            {
                RegisteredStages--;
                stage.AutoCompleted -= OnStageAutoCompleted;
                stage.QuestAutoCompleted -= OnQuestAutoCompleted;
                stage.Updated -= OnStageUpdated;
                stage.Dispose();
                return true;
            }
            return false;
        }

        public event Action<QuestWrapper, NwPlayer, int>? Advanced;
        public event Action<QuestWrapper, NwPlayer>? Completed;
        public event Action<QuestWrapper, NwPlayer>? Updated;


        private void ThrowIfNotOwning(QuestStageWrapper stage)
        {
            if(this[stage.ID] != stage) 
                throw new InvalidOperationException("QuestWrapper subscribed to stage which it does not own.");
        }

        void OnStageAutoCompleted(QuestStageWrapper stage, NwPlayer player, int nextId)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - OnStageAutoCompleted Handler");
            ThrowIfNotOwning(stage);
            Advanced?.Invoke(this, player, nextId);
        }

        void OnQuestAutoCompleted(QuestStageWrapper stage, NwPlayer player)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - OnQuestAutoCompleted Handler");
            ThrowIfNotOwning(stage);
            Completed?.Invoke(this, player);
        }

        void OnStageUpdated(QuestStageWrapper stage, NwPlayer player)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - OnStageUpdated Handler");
            ThrowIfNotOwning(stage);
            Updated?.Invoke(this, player);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach(var stage in _stages.Values)
                stage.Dispose();

            _stages.Clear();
        }

        public QuestStageWrapper? this[int stageId] => _stages.TryGetValue(stageId, out var qw) ? qw : null;
    }
}