using System;
using System.Linq;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveExplore : Objective
    {
        public int AreaExplorePercentage { get; set; }

        internal override ObjectiveExploreWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress(this);
        public override object Clone()
        {
            var areaTags = base.AreaTags.Select(at => (string)at.Clone()).ToArray();
            var triggerTags = base.TriggerTags.Select(at => (string)at.Clone()).ToArray();

            return new ObjectiveExplore()
            {
                NextStageID = base.NextStageID,
                PartyMembersAllowed = base.PartyMembersAllowed,
                JournalEntry = (string)base.JournalEntry.Clone(),
                ShowInJournal = base.ShowInJournal,
                AreaTags = areaTags,
                TriggerTags = triggerTags,
                Cooldown = (ObjectiveTimer?)base.Cooldown?.Clone() ?? null,

                AreaExplorePercentage = this.AreaExplorePercentage,
            };
        }

        private sealed class Progress : IObjectiveProgress
        {
            private readonly ObjectiveExplore _objective;
            public Progress(ObjectiveExplore objective) { _objective = objective; }
            private int percentage = 0;
            public event Action<IObjectiveProgress>? OnUpdate;

            public bool IsCompleted => percentage >= _objective.AreaExplorePercentage;

            public void Proceed(object? parameter)
            {
                if (parameter is int perc && percentage != perc)
                {
                    percentage = perc;
                    OnUpdate?.Invoke(this);
                }
            }

            public string GetProgressString() => IsCompleted ? "(Zbadano obszar)" : $"{percentage}/{_objective.AreaExplorePercentage}";
        }
    }
}