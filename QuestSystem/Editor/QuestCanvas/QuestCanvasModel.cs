using System.Collections.Generic;
using System.Linq;
using QuestSystem;

namespace QuestEditor.QuestCanvas
{
    public sealed class QuestCanvasModel(Quest quest, IEnumerable<QuestStage> stages)
    {
        public readonly Quest Quest = quest;
        public readonly Dictionary<int, QuestStage> Stages = stages.Select((s, i) => new KeyValuePair<int, QuestStage>(i, s)).ToDictionary();
    }
}