using System;
using System.Linq;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveDeliver : Objective
    {
        public string ItemResRef { get; set; } = string.Empty;
        public string ItemTag { get; set; } = string.Empty;
        public int RequiredAmount { get; set; }
        public bool AllowPartialDelivery { get; set; }
        public bool DestroyItemsOnDelivery { get; set; }

        internal override ObjectiveDeliverWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress(this);

        public override object Clone()
        {
            var areaTags = base.AreaTags.Select(at => (string)at.Clone()).ToArray();
            var triggerTags = base.TriggerTags.Select(at => (string)at.Clone()).ToArray();

            return new ObjectiveDeliver()
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
                AllowPartialDelivery = AllowPartialDelivery,
                DestroyItemsOnDelivery = DestroyItemsOnDelivery,
            };
        }

        private sealed class Progress : IObjectiveProgress
        {
            private readonly ObjectiveDeliver _objective;
            public Progress(ObjectiveDeliver objective) { _objective = objective; }
            public event Action<IObjectiveProgress>? OnUpdate;
            private int amount = 0;
            public bool IsCompleted => amount >= _objective.RequiredAmount;
            public void Proceed(object? parameter)
            {
                if (parameter is int amt && amount != amt)
                {
                    amount = amt;
                    OnUpdate?.Invoke(this);
                }
            }
        }
    }
}