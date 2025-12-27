using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace QuestSystem
{
    public sealed class Quest
    {
        private static readonly HashSet<Quest> _loadedQuests = new();
        internal static void RegisterQuest(Quest quest) => _loadedQuests.Add(quest);
        internal static void UnregisterQuest(Quest quest) => _loadedQuests.Remove(quest);
        public static Quest? GetQuest(string tag) => _loadedQuests.FirstOrDefault(q=>string.Equals(q.Tag, tag, StringComparison.OrdinalIgnoreCase));

        public string Tag {get;internal set;} = string.Empty;
        public string Name {get;internal set;} = string.Empty;
        public string JournalEntry {get;internal set;} = string.Empty;

        private readonly HashSet<QuestStage> _stages = new();
        internal void RegisterStage(QuestStage stage) 
        {
            stage.Quest = this;
            _stages.Add(stage);
        }
        internal void UnregisterStage(QuestStage stage)
        {
            stage.Quest = null;
            _stages.Remove(stage);
        }
        
        public QuestStage? GetStage(int stageID) => _stages.FirstOrDefault(s=>s.ID == stageID);

        /// <summary>
        /// Stop tracking all quests progress for this PC.
        /// <br/>If any stage is not tracking players after clear - Unload that stage from memory.
        /// <br/>If any quest has no stages loaded after clear - Unload that quest from memory.
        /// </summary>
        public static void ClearPC(NwCreature pc)
        {
            List<Quest> questsToRemove = new();

            foreach(var quest in _loadedQuests)
            {
                foreach(var stage in quest._stages)
                {
                    if(stage.IsTracking(pc))
                        stage.StopTracking(pc);

                    if(!stage.IsActive)
                    {
                        quest.UnregisterStage(stage);
                    }

                    if(quest._stages.Count == 0)
                        questsToRemove.Add(quest);
                }
            }

            foreach(var qtr in questsToRemove)
                UnregisterQuest(qtr);
        }
    }
}