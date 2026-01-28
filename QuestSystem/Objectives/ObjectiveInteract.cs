using System;
using System.Linq;
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

        internal override ObjectiveInteractWrapper Wrap() => new(this);

        internal override IObjectiveProgress CreateProgressTrack() => new Progress();
        public override object Clone()
        {
            var areaTags = base.AreaTags.Select(at => (string)at.Clone()).ToArray();
            var triggerTags = base.TriggerTags.Select(at => (string)at.Clone()).ToArray();

            return new ObjectiveInteract()
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
                Interaction = this.Interaction,
            };
        }
        private sealed class Progress : IObjectiveProgress
        {
            private bool _interacted = false;
            public bool IsCompleted => _interacted;
            public event Action<IObjectiveProgress>? OnUpdate;
            public void Proceed(object? _ = null)
            {
                if (_interacted) return;
                _interacted = true;
                OnUpdate?.Invoke(this);
            }
        }

    }
}