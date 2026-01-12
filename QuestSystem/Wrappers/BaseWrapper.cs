using System;

namespace QuestSystem.Wrappers
{
    internal abstract class BaseWrapper : IDisposable
    {
        private bool disposed = false;
        public virtual void Dispose()
        {
            NLog.LogManager.GetCurrentClassLogger().Warn($" > > > DISPOSING {GetType().Name} ");
            ObjectDisposedException.ThrowIf(disposed, this);
            disposed = true;
        }
    }
}