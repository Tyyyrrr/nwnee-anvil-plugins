

using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using NLog;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly Quest _quest;

        public string Tag => _quest.Tag;
        public string Name => _quest.Name;

        public QuestWrapper(Quest quest)
        {
            _quest = quest;
        }

        private readonly List<QuestStageWrapper> _stages = new();
        public IReadOnlyList<QuestStageWrapper> Stages => _stages;

        public event Action<QuestWrapper, NwPlayer, int>? MovingToTheNextStage;

        /// <summary>
        /// Store the stage in RAM memory
        /// </summary>
        /// <param name="stage"></param>
        /// <returns>True if successfully added. False if stage index is less than 0, or quest already has any stage with the same ID registered.</returns>
        internal bool RegisterStage(QuestStageWrapper stage)
        {
            if (stage.ID < 0)
            {
                _log.Error("Can't register stage with ID " + stage.ID.ToString());
                return false;
            }

            int index = 0;
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].ID == stage.ID)
                    return false;

                if (_stages[i].ID > stage.ID)
                {
                    index = i;
                    break;
                }
            }

            stage.Quest = this;

            _stages.Insert(index, stage);

            stage.Completed += OnStageCompleted;

            _log.Info($"Stage {stage.ID} of quest \'{Tag}\' registered successfully.");

            return true;
        }

        private void OnStageCompleted(QuestStageWrapper wrapper, NwPlayer player, int nextStageId)
        {
            // move to the next stage before unregistering the previous, to avoid re-caching entire quest
            MovingToTheNextStage?.Invoke(this, player, nextStageId);

            if (!wrapper.IsActive && !UnregisterStage(wrapper))
            {
                _log.Error("Completed stage was not registered.");
            }
        }

        /// <summary>
        /// Clear the stage from RAM memory
        /// </summary>
        /// <returns>True if the stage was stored in RAM, false otherwise</returns>
        internal bool UnregisterStage(QuestStageWrapper stage)
        {
            var q = stage.Quest;

            stage.Quest = null;

            stage.Completed -= OnStageCompleted;

            var res = _stages.Remove(stage) && q != null;

            if(!res) _log.Error($"Failed to unregister stage.");
            else _log.Info($"Stage {stage.ID} of quest \'{q!.Name}\' ({q.Tag}) unregistered successfully.");
            return res;
        }

        public QuestStageWrapper? GetStage(int stageID) => _stages.FirstOrDefault(s => s.ID == stageID);
    }
}