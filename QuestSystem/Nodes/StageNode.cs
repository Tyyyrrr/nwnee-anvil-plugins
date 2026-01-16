using System;
using QuestSystem.Objectives;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Nodes
{
    public sealed class StageNode : NodeBase
    {
        public override bool Rollback { get => true; }
        public string JournalEntry { get; set; } = string.Empty;
        public bool ShowInJournal { get; set; } = true;
        public RewardNode Reward { get; set; } = new();
        public Objective[] Objectives { get; set; } = Array.Empty<Objective>();

        internal override StageNodeWrapper Wrap() => new(this);
    }
}