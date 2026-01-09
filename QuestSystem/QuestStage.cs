using System;
using QuestSystem.Objectives;

namespace QuestSystem
{

    public sealed class QuestStage
    {
        public int ID { get; set; }
        public int NextStageID { get; set; }
        public string JournalEntry { get; set; } = string.Empty;
        public bool ShowInJournal { get; set; } = true;
        public QuestStageReward Reward { get; set; } = new();
        public Objective[] Objectives { get; set; } = Array.Empty<Objective>();
    }
}