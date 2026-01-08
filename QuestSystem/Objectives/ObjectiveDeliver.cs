using System;
using QuestSystem.Wrappers;
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