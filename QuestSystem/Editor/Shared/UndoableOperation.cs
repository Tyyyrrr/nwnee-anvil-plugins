using System.Diagnostics;

namespace QuestEditor.Shared
{
    public abstract class UndoableOperation(IStateful origin) : IUndoable
    {
        public virtual IStateful Origin { get; } = origin;

        private bool _isDone = false;

        public void Do()
        {
            Trace.WriteLine($"Do {GetType().Name} ({Origin.GetType().Name})");
            if (_isDone) throw new InvalidOperationException("Can't call 'Do()' on undoable operation more than once.");
            _isDone = true;
            ProtectedDo();
            Origin.Counter++;
            Origin.RefreshIsDirty();
        }
        protected abstract void ProtectedDo();
        public void Redo()
        {
            Trace.WriteLine($"Redo {GetType().Name} ({Origin.GetType().Name})");
            ProtectedRedo();
            Origin.Counter++;
            Origin.RefreshIsDirty();
        }
        protected abstract void ProtectedRedo();
        public void Undo()
        {
            Trace.WriteLine($"Undo {GetType().Name} ({Origin.GetType().Name})");
            ProtectedUndo();
            Origin.Counter--;
            Origin.RefreshIsDirty();
        }
        protected abstract void ProtectedUndo();
    }
}
