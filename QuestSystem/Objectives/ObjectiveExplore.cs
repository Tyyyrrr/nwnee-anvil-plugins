using System;
using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveExplore : Objective
    {
        public int AreaExplorePercentage { get; set; }

        internal override ObjectiveExploreWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress(this);

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
        }
    }
}