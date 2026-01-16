using System.Collections.Generic;

using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Nodes
{
    public sealed class RewardNode : NodeBase
    {
        public bool NotifyPlayer { get; set; } = true;
        public int Xp { get; set; }
        public int Gold { get; set; }
        public int GoodEvilChange { get; set; }
        public int LawChaosChange { get; set; }
        public Dictionary<string, int> Items { get; set; } = new();
        public bool IsEmpty => 
            Xp == 0 && 
            Gold == 0 && 
            GoodEvilChange == 0 && 
            LawChaosChange == 0 && 
            Items.Count == 0;

        internal override RewardNodeWrapper Wrap() => new(this);
    }
}