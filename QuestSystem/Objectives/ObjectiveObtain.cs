using System;
using System.Linq;
using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveObtain : Objective
    {
        public string ItemResRef { get; set; } = string.Empty;
        public string ItemTag { get; set; } = string.Empty;
        public int RequiredAmount { get; set; }

        internal override ObjectiveObtainWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress(this);
        public override object Clone()
        {
            var areaTags = base.AreaTags.Select(at => (string)at.Clone()).ToArray();
            var triggerTags = base.TriggerTags.Select(at => (string)at.Clone()).ToArray();

            return new ObjectiveObtain()
            {
                NextStageID = base.NextStageID,
                PartyMembersAllowed = base.PartyMembersAllowed,
                JournalEntry = (string)base.JournalEntry.Clone(),
                ShowInJournal = base.ShowInJournal,
                AreaTags = areaTags,
                TriggerTags = triggerTags,
                Cooldown = (ObjectiveTimer?)base.Cooldown?.Clone() ?? null,

                ItemResRef = this.ItemResRef,
                ItemTag = this.ItemTag,
                RequiredAmount = this.RequiredAmount,
            };
        }
        private sealed class Progress : IObjectiveProgress
        {
            private readonly ObjectiveObtain _objective;
            public Progress(ObjectiveObtain objective) { _objective = objective; }
            public event Action<IObjectiveProgress>? OnUpdate;
            int amount = 0;

            public bool IsCompleted => amount >= _objective.RequiredAmount;

            public void Proceed(object? parameter)
            {
                if (parameter is int amt && amt != amount)
                {
                    amount = amt;
                    OnUpdate?.Invoke(this);
                }
            }

            public string GetProgressString() => IsCompleted ? "(W posiadaniu)" : $"{amount}/{_objective.RequiredAmount}";
        }
    }
}