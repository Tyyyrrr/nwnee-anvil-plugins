using System.Diagnostics;
using System.Windows.Input;

namespace QuestEditor.Shared
{
    public abstract class StatefulViewModelBase : ViewModelBase, IStateful
    {

        private readonly StatefulViewModelBase? _parent;

        public event Action<UndoableOperation>? Recorded;

        public event Action<IStateful, IReadOnlyList<IStateful>>? Discarded;

        public ICommand DiscardChangesCommand { get; }

        public StatefulViewModelBase(StatefulViewModelBase? parent = null)
        {
            _parent = parent;
            DiscardChangesCommand = new RelayCommand(_ => Discard(), _ => IsDirty);
        }

        public int Counter { get; set; }
        public virtual bool IsDirty => Counter != 0;

        protected void PushOperation(UndoableOperation op)
        {
            if(_parent != null)
            {
                Trace.WriteLine($"{GetType().Name} bubbling up operation {op.GetType().Name}");
                _parent.PushOperation(op);
            }
            else Recorded?.Invoke(op);
        }

        public void Discard()
        {
            Trace.WriteLine($"Discard {GetType().Name}");
            BubbleDiscard(this, RecursiveDiscard());
        }

        protected abstract IReadOnlyList<StatefulViewModelBase>? DirectDescendants { get; }
        public IReadOnlyList<IStateful> RecursiveDiscard()
        {

            Trace.WriteLine($"Recursive discard {GetType().Name}");
            Counter = 0;
            if (DirectDescendants == null || DirectDescendants.Count == 0)
                return [this];
            
            List<IStateful> discarded = [this];
            foreach (var dd in DirectDescendants.Where(d=>d.IsDirty))
                discarded.AddRange(dd.RecursiveDiscard());

            return discarded;
        }

        private void BubbleDiscard(IStateful origin, IReadOnlyList<IStateful> descendants)
        {
            if (_parent != null)
            {
                Trace.WriteLine($"{GetType().Name} bubbling up discard");
                _parent.BubbleDiscard(origin, descendants);
            }
            else Discarded?.Invoke(origin, [.. descendants]);
        }

        public void RecursiveApply()
        {
            Trace.WriteLine("RecursiveApply " + GetType().Name);
            if(DirectDescendants != null)
                foreach(var dd in DirectDescendants.Where(d=>d.IsDirty))
                    dd.RecursiveApply();
            Apply();
            Counter = 0;
            if (DirectDescendants != null)
                RefreshIsDirty();
        }
        protected virtual void Apply() { }

        public virtual void RefreshIsDirty()
        {
            Trace.WriteLine($"{GetType().Name} refresh IsDirty ({IsDirty})");
            RaisePropertyChanged(nameof(IsDirty));
            ((RelayCommand)DiscardChangesCommand).RaiseCanExecuteChanged();
            _parent?.RefreshIsDirty();
        }
    }
}
