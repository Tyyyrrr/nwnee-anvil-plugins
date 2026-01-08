using System;
using QuestSystem.Objectives;

namespace QuestSystem.Wrappers
{
    internal interface IObjectiveProgress
    {
        public event Action<IObjectiveProgress>? OnUpdate;

        public void Proceed(object? parameter);
        public bool IsCompleted(Objective objective);
    }
}