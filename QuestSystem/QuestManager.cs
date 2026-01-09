using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using NLog;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    internal sealed class QuestManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, QuestWrapper> _loadedQuests = new();
        private readonly Dictionary<NwPlayer, Dictionary<string, int>> _completedQuests = new();

        public event Action<NwPlayer, string, int>? QuestMovingToTheNextStage;

        /// <summary>
        /// Cache the quest in RAM
        /// </summary>
        /// <returns>False if the object was already cached</returns>
        public bool RegisterQuest(QuestWrapper quest)
        {
            _log.Info($"Registering quest \'{quest.Name}\' ({quest.Tag})");
            var res = _loadedQuests.TryAdd(quest.Tag, quest);
            if (res) quest.MovingToTheNextStage += OnQuestMovingToTheNextStage;
            else _log.Error("Failed to register quest.");
            return res;
        }

        /// <summary>
        /// Clear the quest from RAM
        /// </summary>
        /// <returns>False if the object was not in the cache</returns>
        public bool UnregisterQuest(QuestWrapper quest)
        {
            _log.Info($"Unregistering quest \'{quest.Name}\' ({quest.Tag})");
            var res = _loadedQuests.Remove(quest.Tag);
            if(res) quest.MovingToTheNextStage -= OnQuestMovingToTheNextStage;
            else _log.Error("Failed to unregister quest.");
            return res;
        }

        private void OnQuestMovingToTheNextStage(QuestWrapper wrapper, NwPlayer player, int nextStageId)
        {
            string tag = wrapper.Tag;

            if(wrapper.Stages.Count == 0)
                UnregisterQuest(wrapper);

            if(nextStageId < 0) return;

            _log.Info($"Quest {tag} moving to the next stage {nextStageId}");
            QuestMovingToTheNextStage?.Invoke(player, tag, nextStageId);
        }

        /// <returns>Quest object with specified tag if it is already loaded into memory, null otherwise</returns>
        public QuestWrapper? GetCachedQuest(string tag) => _loadedQuests.TryGetValue(tag, out var quest) ? quest : null;

        /// <summary>
        /// Stop tracking all quests progress for the player.
        /// <br/>If any stage is not tracking players after clear - Unload that stage from memory.
        /// <br/>If any quest has no stages loaded after clear - Unload that quest from memory.
        /// </summary>
        public void ClearPlayer(NwPlayer player)
        {
            _log.Warn($"Clearing player {(player.IsValid ? player.PlayerName : "<INVALID>")}");

            List<QuestWrapper> questsToRemove = new();

            foreach (var quest in _loadedQuests.Values)
            {
                List<QuestStageWrapper> stagesToRemove = new();

                foreach (var stage in quest.Stages)
                {
                    if (stage.IsTracking(player))
                        stage.StopTracking(player);

                    if (!stage.IsActive)
                        stagesToRemove.Add(stage);
                }

                foreach (var stage in stagesToRemove)
                    quest.UnregisterStage(stage);

                if (quest.Stages.Count == 0)
                    questsToRemove.Add(quest);
            }

            foreach (var qtr in questsToRemove)
                UnregisterQuest(qtr);

            _ = _completedQuests.Remove(player);
        }

        public void MarkQuestAsCompleted(NwPlayer player, string questTag, int stageId)
        {
            _log.Info($"Marking quest {questTag} as completed on stage {stageId} by player {(player.IsValid ? player.PlayerName : "<INVALID>")}");

            if (!player.IsValid)
            {
                ClearPlayer(player);
                return;
            }

            if (!_completedQuests.TryGetValue(player, out var quests))
            {
                quests = new();
                _completedQuests.Add(player, quests);
            }

            if (quests.TryGetValue(questTag, out var id))
            {
                if (stageId != id)
                {
                    _log.Warn($@"Marking quest \'{questTag}\' as completed on stage {stageId},
                    but player {player.PlayerName} has already completed this quest on stage {id}. Overriding!");

                    quests[questTag] = stageId;
                }
            }
            else quests.Add(questTag, stageId);
        }

        public bool ClearCompletedQuest(NwPlayer player, string questTag)
        {
            return _completedQuests.TryGetValue(player, out var quests) && quests.Remove(questTag);
        }
        public bool HasCompletedQuest(NwPlayer player, string questTag, out int stageId)
        {
            stageId = -1;
            return _completedQuests.TryGetValue(player, out var quests) && quests.TryGetValue(questTag, out stageId);
        }
    }
}