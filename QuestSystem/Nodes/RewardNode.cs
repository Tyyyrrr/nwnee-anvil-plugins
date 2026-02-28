using System.Collections.Generic;
using System.Linq;
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
        public override object Clone()
        {
            var items = Items.Select(i=>new KeyValuePair<string, int>((string)i.Key.Clone(), i.Value)).ToDictionary();

            return new RewardNode()
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback,

                NotifyPlayer = this.NotifyPlayer,
                Xp = this.Xp,
                Gold = this.Gold,
                GoodEvilChange = this.GoodEvilChange,
                LawChaosChange = this.LawChaosChange,
                Items = items
            };
        }
        internal override RewardNodeWrapper Wrap() => new(this);
    }
}