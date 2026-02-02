using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Objectives;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using QuestSystem.Objectives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuestEditor.Nodes
{
    public sealed class StageNodeVM : NodeVM
    {
        private static void DebugObjective(Objective o)
        {
            Trace.WriteLine($"{o} NextID: {o.NextStageID}");
        }
        public StageNodeVM(StageNode node, QuestVM quest) : base(node, quest)
        {
            _outputVM = new(node.ID, node.NextID);
            InputVM.SocketColorBrush = (SolidColorBrush)((App)Application.Current).Resources["StageNodeInputSocketBrush"];
            foreach (var obj in node.Objectives)
            {
                var vm = ObjectiveVM.SelectViewModel(obj, this);
                if (vm == null) continue;
                Objectives.Add(vm);
                vm.OutputChanged += OnObjectiveOutputChanged;
                DebugObjective(obj);
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

        protected override void Apply()
        {
            Node.Objectives = [.. Objectives.Select(o => o.Objective)];
            Trace.WriteLine("Saving objectives");
            foreach (var o in Objectives)
            {
                DebugObjective(o.Objective);
            }
            base.Apply();
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
                stageVM.Objectives.Add(viewModel!);
                stageVM.Node.Objectives = [.. stageVM.Objectives.Select(o => o.Objective)];
                viewModel!.OutputChanged += stageVM.OnObjectiveOutputChanged;
            }
            protected override void ProtectedUndo()
            {
                stageVM.Objectives.Remove(viewModel!);
                stageVM.Node.Objectives = [.. stageVM.Objectives.Select(o => o.Objective)];
                viewModel!.OutputChanged -= stageVM.OnObjectiveOutputChanged;
            }
        }

        private sealed class RemoveObjectiveOperation(StageNodeVM stageVM, ObjectiveVM objective) : UndoableOperation(stageVM)
        {
            private readonly ObjectiveVM _viewModel = objective;

            protected override void ProtectedDo()
            {
                stageVM.Objectives.Remove(_viewModel);
                stageVM.Node.Objectives = [.. stageVM.Objectives.Select(o => o.Objective)];
                stageVM.Node.Objectives = stageVM.Objectives.Select(o => o.Objective).ToArray();
            }
            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                stageVM.Objectives.Add(_viewModel);
                stageVM.Node.Objectives = [.. stageVM.Objectives.Select(o => o.Objective)];
                stageVM.Node.Objectives = [.. stageVM.Node.Objectives, _viewModel.Objective];
            }
        }

        void OnObjectiveOutputChanged(ObjectiveVM objective, int nextID)
        {
            var index = Objectives.IndexOf(objective);
            Trace.WriteLine("On objective output changed. Output socket no. " + index.ToString());
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


        protected override void SetNextOutputTargetID(int nextID, int outputIndex)
        {
            var objective = Objectives.FirstOrDefault(o => o.OutputVM == OutputVMs[outputIndex]);
            if (objective == null) return;
            objective.Objective.NextStageID = nextID;
        }

    }
}