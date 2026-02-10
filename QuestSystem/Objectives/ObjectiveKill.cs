using System;
using System.Linq;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveKill : Objective
    {
        public string ResRef { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public int Amount { get; set; }

        internal override ObjectiveKillWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress(this);
        public override object Clone()
        {
            var areaTags = base.AreaTags.Select(at => (string)at.Clone()).ToArray();
            var triggerTags = base.TriggerTags.Select(at => (string)at.Clone()).ToArray();

            return new ObjectiveKill()
            {
                NextStageID = base.NextStageID,
                PartyMembersAllowed = base.PartyMembersAllowed,
                JournalEntry = (string)base.JournalEntry.Clone(),
                ShowInJournal = base.ShowInJournal,
                AreaTags = areaTags,
                TriggerTags = triggerTags,
                Cooldown = (ObjectiveTimer?)base.Cooldown?.Clone() ?? null,

                ResRef = this.ResRef,
                Tag = this.Tag,
                Amount = this.Amount,
            };
        }
        private sealed class Progress : IObjectiveProgress
        {
            private readonly ObjectiveKill _objective;
            public Progress(ObjectiveKill objective) { _objective = objective; }
            private int _kills = 0;
            public bool IsCompleted => _kills >= _objective.Amount;
            public event Action<IObjectiveProgress>? OnUpdate;
            public void Proceed(object? _ = null)
            {
                _kills++;
                OnUpdate?.Invoke(this);
            }

            public string GetProgressString() => IsCompleted ? "(Ukończono)" : $"{_kills}/{_objective.Amount}";

            public object? GetProgressValue() => _kills;
        }
    }
}