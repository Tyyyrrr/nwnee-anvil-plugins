using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace QuestEditor.Explorer
{
    public sealed class QuestVM : StatefulViewModelBase, ISelectable
    {
        public readonly Quest Model;
        public readonly QuestPackVM PackVM;
        private readonly PackManager _packManager;

        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = false;

        public override bool IsDirty => base.IsDirty || Nodes.Any(n => n.IsDirty);

        public string DisplayText => IsDirty ? $"{QuestTag}*" : QuestTag;
        public FontWeight DisplayFontWeight => IsDirty ? FontWeights.Bold : FontWeights.Regular;
        public FontStyle DisplayFontStyle => IsDirty ? FontStyles.Italic : FontStyles.Normal;

        public string QuestTag
        { 
            get => _questTag;
            set { if (SetProperty(ref _questTag, value)) Model.Name = _questTag; }
        }
        private string _questTag;
        public string Title
        {
            get => _title;
            set { if (SetProperty(ref _title, value)) Model.Name = _title; }
        }
        private string _title;

        public ObservableCollection<NodeVM> Nodes { get; } = [];

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => Nodes;

        public ICommand AddStageNodeCommand { get; }
        public ICommand AddRewardNodeCommand { get; }
        //public ICommand AddRandomizerNodeCommand { get; }
        //public ICommand AddCooldownNodeCommand { get; }
        //...

        public ICommand DeleteQuestCommand { get; }



        public QuestVM(Quest quest, QuestPackVM packVM, PackManager packManager) : base(packVM)
        {
            Model = quest;
            PackVM = packVM;
            _packManager = packManager;
            _packManager.NodesLoadCompleted += OnLoadCompleted;

            _questTag = quest.Tag;
            _title = quest.Name;

            AddStageNodeCommand = new RelayCommand(AddStageNode, _ => IsSelected);
            AddRewardNodeCommand = new RelayCommand(AddRewardNode, _ => IsSelected);
            //...

            DeleteQuestCommand = new RelayCommand(PackVM.DeleteQuest, _ => true);

            _packManager.LoadAllNodes(quest);

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

        int GetNextNodeID()
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
        void AddStageNode(object? _)
        {
            int nextID = GetNextNodeID();
            var model = new StageNode() { ID = nextID, JournalEntry = $"Stage Node {nextID}" };
            PushOperation(new AddNodeOperation<StageNode>(model, this));
        }

        void AddRewardNode(object? _)
        {
            int nextID = GetNextNodeID();
            var model = new RewardNode() { ID = nextID };
            PushOperation(new AddNodeOperation<RewardNode>(model, this));
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