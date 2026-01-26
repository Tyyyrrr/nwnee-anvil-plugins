using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestEditor.Shared
{
    public abstract class StatefulViewModelBase : ViewModelBase, IUndoable
    {
        public event Action<StatefulViewModelBase>? Recorded;

        protected void PushOperation(IUndoable op)
        {
            _undo.Push(op);
            _redo.Clear();
            Recorded?.Invoke(this);
        }

        private readonly Stack<IUndoable> _undo = [];
        private readonly Stack<IUndoable> _redo = [];
        public bool CanUndo => _undo.Count != 0;
        public bool CanRedo => _redo.Count != 0;

        public void Undo()
        {
            if(!CanUndo) return;
            var op = _undo.Pop();
            op.Undo();
            _redo.Push(op);
        }
        public void Redo()
        {
            if (!CanRedo) return;
            var op = _redo.Pop();
            op.Redo();
            _undo.Push(op);
        }
    }
}
