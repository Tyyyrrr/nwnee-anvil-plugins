using System;
using System.Linq;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveSpellcast : Objective
    {
        public string TargetResRef { get; set; } = string.Empty;
        public string TargetTag { get; set; } = string.Empty;
        public int SpellID { get; set; } = -1;

        internal override ObjectiveSpellcastWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress();
        public override object Clone()
        {
            var areaTags = base.AreaTags.Select(at => (string)at.Clone()).ToArray();
            var triggerTags = base.TriggerTags.Select(at => (string)at.Clone()).ToArray();

            return new ObjectiveSpellcast()
            {
                NextStageID = base.NextStageID,
                PartyMembersAllowed = base.PartyMembersAllowed,
                JournalEntry = (string)base.JournalEntry.Clone(),
                ShowInJournal = base.ShowInJournal,
                AreaTags = areaTags,
                TriggerTags = triggerTags,
                Cooldown = (ObjectiveTimer?)base.Cooldown?.Clone() ?? null,

                TargetResRef = this.TargetResRef,
                TargetTag = this.TargetTag,
                SpellID = this.SpellID,
            };
        }
        private sealed class Progress : IObjectiveProgress
        {
            private bool _completed = false;
            public bool IsCompleted => _completed;
            public event Action<IObjectiveProgress>? OnUpdate;

            public string GetProgressString() => IsCompleted ? "(Zaklęcie rzucone)" : string.Empty;

            public void Proceed(object? _)
            {
                if (_completed == false)
                {
                    _completed = true;
                    OnUpdate?.Invoke(this);
                }
            }
            
            public object? GetProgressValue() => _completed;
        }

    }
}