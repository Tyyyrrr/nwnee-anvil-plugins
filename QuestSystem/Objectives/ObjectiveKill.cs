using System;
using QuestSystem.Wrappers;
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
        }
    }
}