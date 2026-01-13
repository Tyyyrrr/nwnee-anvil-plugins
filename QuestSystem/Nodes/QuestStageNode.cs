using System;
using QuestSystem.Objectives;

namespace QuestSystem.Nodes
{
    public sealed class QuestStageNode : NodeBase
    {
        public string JournalEntry { get; set; } = string.Empty;
        public bool ShowInJournal { get; set; } = true;
        public QuestStageReward Reward { get; set; } = new();
        public Objective[] Objectives { get; set; } = Array.Empty<Objective>();
    }
}