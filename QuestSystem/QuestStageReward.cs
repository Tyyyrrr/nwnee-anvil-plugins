using System.Collections.Generic;

namespace QuestSystem
{
    public sealed class QuestStageReward
    {
        public bool NotifyPlayer { get; set; } = true;
        public int Xp { get; set; }
        public int Gold { get; set; }
        public int GoodEvilChange { get; set; }
        public int LawChaosChange { get; set; }
        public Dictionary<string, int> Items { get; set; } = new();
        public Dictionary<string, bool> ObjectVisibility { get; set; } = new();
    }
}