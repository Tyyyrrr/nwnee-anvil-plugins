using System.Collections.Generic;
using QuestSystem;

namespace QuestEditor.QuestCanvas
{
    public sealed class QuestCanvasModel
    {
        public readonly Quest Quest;
        public readonly List<QuestStage> Stages = [];

        public QuestCanvasModel(Quest quest, IEnumerable<QuestStage> stages)
        {
            Quest = quest;
            Stages = [..stages];
        }
    }
}