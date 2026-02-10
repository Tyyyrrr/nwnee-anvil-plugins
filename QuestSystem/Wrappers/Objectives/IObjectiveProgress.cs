using System;

namespace QuestSystem.Wrappers.Objectives
{
    internal interface IObjectiveProgress
    {
        public event Action<IObjectiveProgress>? OnUpdate;
        public void Proceed(object? parameter = null);
        public string GetProgressString();
        public object? GetProgressValue();
        public bool IsCompleted { get; }
    }
}