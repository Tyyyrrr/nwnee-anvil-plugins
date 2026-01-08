using System.Collections.Generic;
using System.IO;

namespace QuestSystem
{
    public sealed class QuestStageReward
    {
        public static string? Serialize(QuestStageReward questStageReward) => QuestSerializer.Serialize(questStageReward);
        public static QuestStageReward? Deserialize(string json) => QuestSerializer.Deserialize<QuestStageReward>(json);
        public static QuestStageReward? Deserialize(Stream stream) => QuestSerializer.Deserialize<QuestStageReward>(stream);

        public bool NotifyPlayer { get; set; } = true;
        public int Xp { get; set; }
        public int Gold { get; set; }
        public int GoodEvilChange { get; set; }
        public int LawChaosChange { get; set; }
        public Dictionary<string, int> Items { get; set; } = new();
        public Dictionary<string, bool> ObjectVisibility { get; set; } = new();
    }
}