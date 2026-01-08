using System;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveKill : Objective
    {
        public string ResRef { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public int Amount { get; set; }

        internal sealed class Progress : IObjectiveProgress
        {
            private int _kills = 0;
            public event Action<IObjectiveProgress>? OnUpdate;
            public bool IsCompleted(Objective objective) => _kills >= ((ObjectiveKill)objective).Amount;
            public void Proceed(object? _ = null)
            {
                _kills++;
                OnUpdate?.Invoke(this);
            }
        }
        internal override IObjectiveProgress CreateProgressTrack() => new Progress();

        internal override ObjectiveKillWrapper Wrap() => new(this);
    }
}