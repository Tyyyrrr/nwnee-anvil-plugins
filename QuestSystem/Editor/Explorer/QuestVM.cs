using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace QuestEditor.Explorer
{
    public sealed class QuestVM : StatefulViewModelBase, ISelectable
    {
        public readonly Quest Model;
        public readonly QuestPackVM PackVM;
        private readonly PackManager _packManager;

        public static event Action<QuestVM>? SelectionCleared;

        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = false;

        public override bool IsDirty => base.IsDirty || Nodes.Any(n => n.IsDirty);

        public string DisplayText => IsDirty ? $"{QuestTag}*" : QuestTag;
        public FontWeight DisplayFontWeight => IsDirty ? FontWeights.Bold : FontWeights.Regular;
        public FontStyle DisplayFontStyle => IsDirty ? FontStyles.Italic : FontStyles.Normal;

        public string QuestTag
        { 
            get => _questTag;
            set { if (SetProperty(ref _questTag, value)) Model.Tag = _questTag; }
        }
        private string _questTag;
        public string Title
        {
            get => Model.Name;
            set
            {
                if (Title != value)
                {
                    PushOperation(new SetQuestTitleOperation(this, value));
                }
            }
        }

        private sealed class SetQuestTitleOperation(QuestVM vm, string title) : UndoableOperation(vm)
        {
            private readonly string _backup = vm.Title;
            protected override void ProtectedDo()
            {
                vm.Model.Name = title;
                vm.RaisePropertyChanged(nameof(Title));
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                vm.Model.Name = _backup;
                vm.RaisePropertyChanged(nameof(Title));
            }
        }




        public ObservableCollection<NodeVM> Nodes { get; } = [];

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => Nodes;

        public ICommand AddStageNodeCommand { get; }
        public ICommand AddRewardNodeCommand { get; }
        public ICommand AddRandomizerNodeCommand { get; }
        public ICommand AddCooldownNodeCommand { get; }
        public ICommand AddVisibilityNodeCommand { get; }
        //...

        public ICommand DeleteQuestCommand { get; }
        public ICommand RenameQuestCommand { get; }



        public QuestVM(Quest quest, QuestPackVM packVM, PackManager packManager) : base(packVM)
        {
            Model = quest;
            PackVM = packVM;
            _packManager = packManager;
            _packManager.NodesLoadCompleted += OnLoadCompleted;

            _questTag = quest.Tag;

            AddStageNodeCommand = new RelayCommand(AddStageNode, _ => IsSelected);
            AddRewardNodeCommand = new RelayCommand(AddRewardNode, _ => IsSelected);
            AddRandomizerNodeCommand = new RelayCommand(AddRandomizerNode, _ => IsSelected);
            AddCooldownNodeCommand = new RelayCommand(AddCooldownNode, _ => IsSelected);
            AddVisibilityNodeCommand = new RelayCommand(AddVisibilityNode, _ => IsSelected);
            //...

            DeleteQuestCommand = new RelayCommand(PackVM.DeleteQuest, _ => true);
            RenameQuestCommand = new RelayCommand(RenameBegin, _ => true);

            _packManager.LoadAllNodes(quest);

        }

        public event Action<QuestVM, string, string>? Renamed;
        void RenameBegin(object? _)
        {
            var dlg = new RenameQuestPopupWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = this
            };

            Rename = QuestTag;

            if (dlg.ShowDialog() == true)
                Renamed?.Invoke(this, QuestTag, Rename);
            
            Rename = string.Empty;
        }

        public string Rename
        {
            get => _rename;
            set => SetProperty(ref _rename, value);
        } private string _rename = string.Empty;


        public void Subscribe()
        {
            _packManager.NodesLoadCompleted += OnLoadCompleted;
        }
        public void Unsubscribe()
        {
            _packManager.NodesLoadCompleted -= OnLoadCompleted;
            _packManager.MetadataReadCompleted -= OnMetadataLoadCompleted;
        }

        public override void RefreshIsDirty()
        {
            base.RefreshIsDirty();
            RaisePropertyChanged(nameof(DisplayFontStyle));
            RaisePropertyChanged(nameof(DisplayFontWeight));
            RaisePropertyChanged(nameof(DisplayText));
        }


        private sealed class AddNodeOperation<T>(T model, QuestVM quest) : UndoableOperation(quest) where T : NodeBase
        {
            private readonly T _model = model;
            private NodeVM? viewModel;
            protected override void ProtectedDo()
            {
                viewModel = NodeVM.SelectViewModel(_model, quest) ?? throw new NotImplementedException($"View of the model \'{typeof(T).Name}\' is not implemented");
                ProtectedRedo();
            }
            protected override void ProtectedRedo()
            {
                var questVM = (QuestVM)Origin;
                questVM.Nodes.Add(viewModel!);
                questVM._packManager.WriteNode(questVM.Model, viewModel!.Model);
            }
            protected override void ProtectedUndo()
            {
                var questVM = (QuestVM)Origin;
                questVM.Nodes.Remove(viewModel!);
                questVM._packManager.RemoveNode(questVM.Model.Tag, viewModel!.Model.ID);
            }
        }

        private sealed class RemoveNodeOperation(NodeVM nodeVM, QuestVM quest) : UndoableOperation(quest)
        {
            private readonly NodeVM _viewModel = nodeVM;

            protected override void ProtectedDo()
            {
                var questVM = (QuestVM)Origin;
                questVM.Nodes.Remove(_viewModel);
                questVM._packManager.RemoveNode(questVM.QuestTag, _viewModel.ID);
            }
            protected override void ProtectedUndo()
            {
                var questVM = (QuestVM)Origin;
                questVM.Nodes.Add(_viewModel);
                questVM._packManager.WriteNode(questVM.Model, _viewModel.Model);
            }
            protected override void ProtectedRedo() => ProtectedDo();
        }

        private sealed class RemoveNodesOperation(QuestVM quest, IEnumerable<NodeVM> nodes) : UndoableOperation(quest)
        {
            private readonly NodeVM[] _viewModels = [..nodes];

            protected override void ProtectedDo() => ProtectedRedo();
            protected override void ProtectedRedo()
            {
                foreach (var node in _viewModels)
                    ((QuestVM)Origin).Nodes.Remove(node);

                Trace.WriteLine("Nodes: " + ((QuestVM)Origin).Nodes.Count);
            }
            protected override void ProtectedUndo()
            {
                foreach (var node in _viewModels)
                    ((QuestVM)Origin).Nodes.Add(node);

                Trace.WriteLine("Nodes: " + ((QuestVM)Origin).Nodes.Count);
            }
        }

        public void RemoveNode(object? parameter)
        {
            if (parameter is not NodeVM nodeVM) throw new InvalidOperationException("Parameter is not a NodeVM");
            PushOperation(new RemoveNodeOperation(nodeVM, this));
        }

        int GetNextStageID()
        {
            int nextID = 0;
            foreach (var n in Nodes.OrderBy(n => n.ID))
            {
                if (nextID >= n.ID)
                    nextID++;
                else break;
            }
            return nextID;
        }

        int GetNextNodeID()
        {
            int nextID = int.MaxValue;
            foreach (var n in Nodes.OrderByDescending(n => n.ID))
            {
                if (nextID <= n.ID)
                    nextID--;
                else break;
            }
            return nextID;
        }
        void AddStageNode(object? _)
        {
            int nextID = GetNextStageID();
            var model = new StageNode() { ID = nextID, JournalEntry = $"Stage Node {nextID}" };
            PushOperation(new AddNodeOperation<StageNode>(model, this));
        }

        void AddRewardNode(object? _)
        {
            int nextID = GetNextNodeID();
            var model = new RewardNode() { ID = nextID };
            PushOperation(new AddNodeOperation<RewardNode>(model, this));
        }

        void AddRandomizerNode(object? _)
        {
            int nextID = GetNextNodeID();
            var model = new RandomizerNode() { ID = nextID };
            PushOperation(new AddNodeOperation<RandomizerNode>(model, this));
        }

        void AddCooldownNode(object? _)
        {
            int nextID = GetNextNodeID();
            var model = new CooldownNode() { ID = nextID };
            PushOperation(new AddNodeOperation<CooldownNode>(model, this));
        }

        void AddVisibilityNode(object? _)
        {
            int nextID = GetNextNodeID();
            var model = new VisibilityNode() { ID = nextID };
            PushOperation(new AddNodeOperation<VisibilityNode>(model, this));
        }

        protected override void Apply()
        {
            Model.Tag = QuestTag;
            Model.Name = Title;
            SaveNodePositions();
            _packManager.RemoveQuest(QuestTag);
            _packManager.WriteQuest(Model);
            foreach (var node in Nodes)
                _packManager.WriteNode(Model,node.Model);
            var nodePositions = JsonSerializer.Serialize(NodePositions);
            _packManager.WriteMetadata(Model, nodePositions);
        }
        public void Select() => IsSelected = true;

        void OnLoadCompleted(string tag, NodeBase[]? nodes)
        {
            if (tag != this.QuestTag) return; // loaded nodes for another quest

            Application.Current.Dispatcher.Invoke(() =>
            {
                Trace.WriteLine($"{this}: all nodes loaded");
                if(nodes == null)
                {
                    Trace.WriteLine("... but operation has failed");
                    return;
                }

                Nodes.Clear();

                int validNodes = 0;
                foreach (var node in nodes)
                {
                    var vm = NodeVM.SelectViewModel(node, this);
                    if (vm == null)
                    {
                        Trace.WriteLine($"Failed to create view model for node {node.ID} ({node.GetType().Name})");
                        continue;
                    }
                    validNodes++;
                    Nodes.Add(vm);
                }
                Trace.WriteLineIf(validNodes != nodes.Length, $"{nodes.Length - validNodes} out of {nodes.Length} loaded nodes were incompatibile");

                _packManager.MetadataReadCompleted += OnMetadataLoadCompleted;
                _packManager.LoadMetadata(Model);
            });
        }

        void OnMetadataLoadCompleted(string tag, object? o)
        {
            if(tag != this.QuestTag) return;

            _packManager.MetadataReadCompleted -= OnMetadataLoadCompleted;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Trace.WriteLine($"{this} metadata loaded");

                if (o != null)
                {
                    Trace.WriteLine("o is of type " + o.GetType().Name);
                    var dict = JsonSerializer.Deserialize<Dictionary<int, Point>>((JsonElement)o);
                    NodePositions = dict!;

                    Trace.WriteLine("dict stored positions: " + NodePositions.Count);

                    foreach (var node in Nodes)
                        if (dict!.TryGetValue(node.ID, out Point point))
                        {
                            node.CanvasPosition = point;
                        }
                        else dict.Add(node.ID, default);
                    return;
                }
                else
                {
                    SaveNodePositions();
                }

            });
        }
        public void ClearSelection()
        {
            IsSelected = false;
            SelectionCleared?.Invoke(this);
        }


        public Dictionary<int, Point> NodePositions { get; private set; } = [];

        public void SaveNodePositions()
        {
            NodePositions.Clear();
            foreach(var node in Nodes)
                NodePositions.Add(node.ID, node.CanvasPosition);
        }
    }
}