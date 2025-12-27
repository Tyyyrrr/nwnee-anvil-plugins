using System;
using QuestSystem.Objectives.Templates;

namespace QuestSystem.Objectives
{
    internal abstract class Objective
    {
        ObjectiveTemplate Template {get; init;}

        public bool Failed 
        {
            get => _failed;
            private set
            {
                if(_failed == value) return;

                _failed = value;

                Updated?.Invoke(this);
            }
        } private bool _failed = false;

        public bool Completed 
        {
            get => _completed;
            private set
            {
                if(_completed == value) return;

                _completed = value;

                Updated?.Invoke(this);
            }
        } private bool _completed = false;

        public abstract void Proceed();
        public void Fail() => Failed = true;

        public event Action<Objective>? Updated;
    }
}