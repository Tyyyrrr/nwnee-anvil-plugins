

using System.Collections.Generic;
using System.Linq;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestWrapper
    {
        private readonly Quest _quest;

        public string Tag => _quest.Tag;
        public string Name => _quest.Name;

        public QuestWrapper(Quest quest)
        {
            _quest = quest;
        }

        private readonly List<QuestStageWrapper> _stages = new();
        internal IReadOnlyList<QuestStageWrapper> Stages => _stages;

        //internal readonly QuestPack? Pack;

        /// <summary>
        /// Store the stage in RAM memory
        /// </summary>
        /// <param name="stage"></param>
        /// <returns>True if successfully added. False if stage index is less than 0, or quest already has any stage with the same ID registered.</returns>
        internal bool RegisterStage(QuestStageWrapper stage)
        {
            if (stage.ID < 0) return false;

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

            return true;
        }

        /// <summary>
        /// Clear the stage from RAM memory
        /// </summary>
        /// <returns>True if the stage was stored in RAM, false otherwise</returns>
        internal bool UnregisterStage(QuestStageWrapper stage)
        {
            stage.Quest = null;
            return _stages.Remove(stage);
        }

        public QuestStageWrapper? GetStage(int stageID) => _stages.FirstOrDefault(s => s.ID == stageID);
    }
}