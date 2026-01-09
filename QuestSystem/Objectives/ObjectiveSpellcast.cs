using System;
using QuestSystem.Wrappers;
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

        private sealed class Progress : IObjectiveProgress
        {
            private bool _completed = false;
            public bool IsCompleted => _completed;
            public event Action<IObjectiveProgress>? OnUpdate;
            public void Proceed(object? _)
            {
                if (_completed == false)
                {
                    _completed = true;
                    OnUpdate?.Invoke(this);
                }
            }
        }

    }
}