using QuestEditor.Explorer;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
namespace QuestEditor.Nodes
{
    internal class VisibilityNodeVM : SingleOutputNodeVM
    {
        public VisibilityNodeVM(VisibilityNode node, QuestVM quest) : base(node, quest)
        {
            AddObjectCommand = new RelayCommand(s => PushOperation(new AddObjectOperation(this, (string)s!)), s => s is string str && !string.IsNullOrEmpty(str) && !Objects.Any(o=>o.StringValue==str));
            RemoveObjectCommand = new RelayCommand(o => PushOperation(new RemoveObjectOperation(this,(StringBoolListItemVM)o!)), o => o is StringBoolListItemVM vm && Objects.Contains(vm));

            foreach (var kvp in node.Objects)
            {
                var li = new StringBoolListItemVM() { StringValue = kvp.Key, BoolValue = kvp.Value };
                Objects.Add(li);
                li.PropertyChanged += OnListItemPropertyChanged;
            }

            IsOutputAvailable = true;
            IsInputAvailable = true;
        }

        protected override void Apply()
        {
            base.Apply();

            Node.Objects = Objects.Select(li => (li.StringValue, li.BoolValue)).ToDictionary();   
        }

        protected override VisibilityNode Node => (VisibilityNode)base.Node;
        public override string NodeType => "Visibility";

        public ObservableCollection<StringBoolListItemVM> Objects { get; private set; } = [];

        public ICommand AddObjectCommand { get; }
        public ICommand RemoveObjectCommand { get; }

        private sealed class AddObjectOperation(VisibilityNodeVM origin, string objectTag) : UndoableOperation(origin)
        {
            private StringBoolListItemVM? listItemVM;
            protected override void ProtectedDo()
            {
                if (origin.Objects.Any(o => o.StringValue == objectTag))
                    return;

                listItemVM = new() { StringValue = objectTag };

                origin.Objects.Add(listItemVM);
                listItemVM.PropertyChanged += origin.OnListItemPropertyChanged;
            }

            protected override void ProtectedRedo()
            {
                if (origin.Objects.Any(o => o.StringValue == objectTag))
                    return;
                origin.Objects.Add(listItemVM!);
                listItemVM!.PropertyChanged += origin.OnListItemPropertyChanged;
            }

            protected override void ProtectedUndo()
            {
                origin.Objects.Remove(listItemVM!);
                listItemVM!.PropertyChanged -= origin.OnListItemPropertyChanged;
            }
        }

        private bool pushLock = false;
        void OnListItemPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (pushLock || s is not StringBoolListItemVM vm || e.PropertyName != nameof(StringBoolListItemVM.BoolValue))
                return;

            pushLock = true;
            PushOperation(new SetObjectVisibilityOperation(this, vm));
            pushLock = false;
        }

        private sealed class SetObjectVisibilityOperation(VisibilityNodeVM origin, StringBoolListItemVM vm) : UndoableOperation(origin)
        {
            private readonly bool _originalValue = !vm.BoolValue;
            protected override void ProtectedDo() { }
            protected override void ProtectedRedo()
            {
                origin.pushLock = true;
                vm.BoolValue = !_originalValue;
                origin.pushLock = false;
            }
            protected override void ProtectedUndo()
            {
                origin.pushLock = true;
                vm.BoolValue = _originalValue;
                origin.pushLock = false;
            }
        }

        private sealed class RemoveObjectOperation(VisibilityNodeVM origin, StringBoolListItemVM vm) : UndoableOperation(origin)
        {
            protected override void ProtectedDo()
            {
                if (!origin.Objects.Remove(vm)) throw new InvalidOperationException("ListItem to remove is missing");
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                if (origin.Objects.Contains(vm)) throw new InvalidOperationException("List item to add is already on the list");

                origin.Objects.Add(vm);
            }
        }

    }
}