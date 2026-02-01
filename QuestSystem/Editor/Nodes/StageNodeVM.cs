using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Objectives;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using QuestSystem.Objectives;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace QuestEditor.Nodes
{
    public sealed class StageNodeVM : NodeVM
    {
        public StageNodeVM(StageNode node, QuestVM quest) : base(node, quest)
        {
            _outputVM = new(node.ID, node.NextID);
            foreach (var obj in node.Objectives)
            {
                var vm = ObjectiveVM.SelectViewModel(obj, this);
                if (vm == null) continue;
                Objectives.Add(vm);
                vm.OutputChanged += OnObjectiveOutputChanged;
            }

            IsInputAvailable = false;
            IsOutputAvailable = true;

            NodeType = "Stage " + node.ID.ToString();

            AddObjectiveInteractCommand = new RelayCommand(AddObjectiveInteract, _ => true);
            AddObjectiveKillCommand = new RelayCommand(AddObjectiveKill, _ => true);
            AddObjectiveDeliverCommand = new RelayCommand(AddObjectiveDeliver, _ => true);
            AddObjectiveObtainCommand = new RelayCommand(AddObjectiveObtain, _ => true);
            AddObjectiveExploreCommand = new RelayCommand(AddObjectiveExplore, _ => true);
            AddObjectiveSpellcastCommand = new RelayCommand(AddObjectiveSpellcast, _ => true);
        }


        public ICommand AddObjectiveInteractCommand { get; }
        void AddObjectiveInteract(object? _) => PushOperation(new AddObjectiveOperation<ObjectiveInteract>(this, new()));
        public ICommand AddObjectiveKillCommand { get; }
        void AddObjectiveKill(object? _) => PushOperation(new AddObjectiveOperation<ObjectiveKill>(this, new()));
        public ICommand AddObjectiveDeliverCommand { get; }
        void AddObjectiveDeliver(object? _) => PushOperation(new AddObjectiveOperation<ObjectiveDeliver>(this, new()));
        public ICommand AddObjectiveObtainCommand { get; }
        void AddObjectiveObtain(object? _) => PushOperation(new AddObjectiveOperation<ObjectiveObtain>(this, new()));
        public ICommand AddObjectiveExploreCommand { get; }
        void AddObjectiveExplore(object? _) => PushOperation(new AddObjectiveOperation<ObjectiveExplore>(this, new()));
        public ICommand AddObjectiveSpellcastCommand { get; }
        void AddObjectiveSpellcast(object? _) => PushOperation(new AddObjectiveOperation<ObjectiveSpellcast>(this, new()));

        public void RemoveObjective(object? parameter)
        {
            if (parameter is not ObjectiveVM objective) return;

            if(this.Objectives.Contains(objective))
                PushOperation(new RemoveObjectiveOperation(this,objective));
        }

        public override string NodeType { get; }
        public override bool CanChangeRollback => false;

        public ConnectionOutputVM OutputVM
        {
            get => _outputVM;
            set => SetProperty(ref _outputVM, value);
        } ConnectionOutputVM _outputVM;

        public override IReadOnlyList<ConnectionOutputVM> OutputVMs => [_outputVM, .. Objectives.Select(o => o.OutputVM)];


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

        private sealed class AddObjectiveOperation<T>(StageNodeVM stageVM, T objective) : UndoableOperation(stageVM) where T : Objective
        {
            private readonly T _model = objective;
            private ObjectiveVM? viewModel;
            protected override void ProtectedDo()
            {
                viewModel = ObjectiveVM.SelectViewModel(_model, stageVM) ?? throw new NotImplementedException($"View of the model \'{typeof(T).Name}\' is not implemented");
                ProtectedRedo();
            }
            protected override void ProtectedRedo()
            {
                var stageVM = (StageNodeVM)Origin;
                stageVM.Objectives.Add(viewModel!);
                viewModel!.OutputChanged += stageVM.OnObjectiveOutputChanged;
            }
            protected override void ProtectedUndo()
            {
                var stageVM = (StageNodeVM)Origin;
                stageVM.Objectives.Remove(viewModel!);
                viewModel!.OutputChanged -= stageVM.OnObjectiveOutputChanged;
            }
        }

        private sealed class RemoveObjectiveOperation(StageNodeVM stageVM, ObjectiveVM objective) : UndoableOperation(stageVM)
        {
            private readonly ObjectiveVM _viewModel = objective;

            protected override void ProtectedDo()
            {
                var vm = (StageNodeVM)Origin;
                vm.Objectives.Remove(_viewModel);
                vm.Node.Objectives = vm.Objectives.Select(o => o.Objective).ToArray();
            }
            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                var vm = (StageNodeVM)Origin;
                vm.Objectives.Add(_viewModel);
                vm.Node.Objectives = [.. vm.Node.Objectives, _viewModel.Objective];
            }
        }

        void OnObjectiveOutputChanged(ObjectiveVM objective, int nextID)
        {
            var index = Objectives.IndexOf(objective);
            RaiseOutputChanged(index, nextID);
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

        public override void SetNextID(int nextID, int outputIndex = 0)
        {
            if (outputIndex == 0)
            {
                base.SetNextID(nextID);
                return;
            }
            
            if(Objectives.Count > 0 && Objectives.Count < outputIndex)
            {
                var obj = Objectives[outputIndex];
                obj.NextStageID = nextID;
            }
        }
    }
}