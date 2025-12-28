using System;

namespace QuestSystem.Objectives
{
    public abstract partial class Objective
    {
        protected internal interface IObjectiveProgress
        {
            public event Action<IObjectiveProgress>? OnUpdate;

            public void Proceed(object? parameter);
            public bool IsCompleted(Objective objective);
        }
    }

}