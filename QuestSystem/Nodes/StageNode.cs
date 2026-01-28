using System;
using System.Collections.Generic;
using System.Linq;
using QuestSystem.Objectives;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Nodes
{
    public sealed class StageNode : NodeBase
    {
        public override bool Rollback { get => true; }
        public string JournalEntry { get; set; } = string.Empty;
        public bool ShowInJournal { get; set; } = true;
        public Objective[] Objectives { get; set; } = Array.Empty<Objective>();

        public override object Clone()
        {
            var objectives = Objectives.Select(o => (Objective)o.Clone()).ToArray();

            return new StageNode()
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback,
                JournalEntry = this.JournalEntry,
                ShowInJournal = this.ShowInJournal,
                Objectives = objectives
            };
        }

        internal override StageNodeWrapper Wrap() => new(this);
    }
}