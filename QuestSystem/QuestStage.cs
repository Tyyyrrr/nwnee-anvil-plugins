using System;
using System.IO;
using QuestSystem.Objectives;

namespace QuestSystem
{

    public sealed class QuestStage
    {
        public static string? Serialize(QuestStage questStage) => QuestSerializer.Serialize(questStage);
        public static QuestStage? Deserialize(string json) => QuestSerializer.Deserialize<QuestStage>(json);
        public static QuestStage? Deserialize(Stream stream) => QuestSerializer.Deserialize<QuestStage>(stream);

        public int ID { get; set; }
        public int NextStageID { get; set; }
        public string JournalEntry { get; set; } = string.Empty;
        public bool ShowInJournal { get; set; } = true;
        public QuestStageReward Reward { get; set; } = new();
        public Objective[] Objectives { get; set; } = Array.Empty<Objective>();
    }
}