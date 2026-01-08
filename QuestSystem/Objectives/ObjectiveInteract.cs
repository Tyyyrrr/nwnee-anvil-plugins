using System;
using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveInteract : Objective
    {
        public enum InteractionType
        {
            PlaceableUse,
            ItemActivate,
            TriggerEnter,
            ObjectExamine
        }

        public InteractionType Interaction { get; set; }

        public string ResRef { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;

        internal sealed class Progress : IObjectiveProgress
        {
            private bool _interacted = false;
            public event Action<IObjectiveProgress>? OnUpdate;

            public bool IsCompleted(Objective _) => _interacted;

            public void Proceed(object? _ = null)
            {
                if (_interacted) return;
                _interacted = true;
                OnUpdate?.Invoke(this);
            }
        }

        internal override IObjectiveProgress CreateProgressTrack() => new Progress();
        internal override ObjectiveInteractWrapper Wrap() => new(this);
    }
}