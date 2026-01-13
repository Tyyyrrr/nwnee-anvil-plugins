using System;

namespace QuestSystem.Wrappers
{
    internal abstract class WrapperBase : IDisposable
    {
        private bool disposed = false;
        public void Dispose()
        {
            if(disposed) return;

            disposed = true;

            NLog.LogManager.GetCurrentClassLogger().Warn($" > > > DISPOSING {GetType().Name} ");
            
            ProtectedDispose();
        }
        protected void ThrowIfDisposed()=>ObjectDisposedException.ThrowIf(disposed, this);
        protected virtual void ProtectedDispose(){}
    }
}