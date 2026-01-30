using QuestEditor.Explorer;
using QuestEditor.Objectives;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;

namespace QuestEditor.Nodes
{
    public sealed class StageNodeVM : NodeVM
    {
        public StageNodeVM(StageNode node, QuestVM quest) : base(node, quest)
        {
            foreach(var obj in node.Objectives)
            {
                var vm = ObjectiveVM.SelectViewModel(obj, this);
                if (vm == null) continue;
                Objectives.Add(vm);
            }
        }

        protected override StageNode Node => (StageNode)base.Node;

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => Objectives;

        public ObservableCollection<ObjectiveVM> Objectives { get; } = [];

        private sealed class SetJournalEntryOperation(StageNodeVM vm, string entry) : UndoableOperation(vm)
        {
            private readonly string _backup = vm.JournalEntry;
            private readonly string _entry = entry;
            protected override void ProtectedDo()
            {
                var vm = (StageNodeVM)Origin;
                vm.Node.JournalEntry = _entry;
                vm.RaisePropertyChanged(nameof(JournalEntry));
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                var vm = (StageNodeVM)Origin;
                vm.Node.JournalEntry = _backup;
                vm.RaisePropertyChanged(nameof(JournalEntry));
            }
        }

        private sealed class ToggleJournalEntryOperation(StageNodeVM vm) : UndoableOperation(vm)
        {
            private bool _initialiValue = vm.ShowInJournal;

            protected override void ProtectedDo()
            {
                var vm = (StageNodeVM)Origin;
                vm.Node.ShowInJournal = !_initialiValue;
                vm.RaisePropertyChanged(nameof(ShowInJournal));
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                var vm = (StageNodeVM)Origin;
                vm.Node.ShowInJournal = _initialiValue;
                vm.RaisePropertyChanged(nameof(ShowInJournal));
            }
        }

        public string JournalEntry
        {
            get => Node.JournalEntry;
            set
            {
                if (Node.JournalEntry == value) return;
                PushOperation(new SetJournalEntryOperation(this, value));
            }
        }

        public bool ShowInJournal
        {
            get => Node.ShowInJournal;
            set
            {
                if(Node.ShowInJournal == value) return;
                PushOperation(new ToggleJournalEntryOperation(this));
            }
        }
    }
}