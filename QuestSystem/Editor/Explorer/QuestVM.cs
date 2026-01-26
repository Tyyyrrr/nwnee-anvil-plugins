using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace QuestEditor.Explorer
{
    public sealed class QuestVM : ViewModelBase, IDisposable, ISelectable
    {
        private readonly Quest _quest;
        private readonly QuestPackVM _packVM;
        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = false;

        public string QuestTag
        { 
            get => _questTag;
            set { if (SetProperty(ref _questTag, value)) _quest.Name = _questTag; }
        }
        private string _questTag;
        public string Title
        {
            get => _title;
            set { if (SetProperty(ref _title, value)) _quest.Name = _title; }
        }
        private string _title;
        public ObservableCollection<NodeVM?> Nodes { get; set; } = [];
        public QuestVM(Quest quest, QuestPackVM packVM)
        {
            _quest = quest;
            _packVM = packVM;

            _questTag = quest.Tag;
            _title = quest.Name;

            var nodeA = new StageNodeVM(new QuestSystem.Nodes.StageNode());
            var nodeB = new RewardNodeVM(new QuestSystem.Nodes.RewardNode());
            Nodes.Add(nodeA);
            Nodes.Add(nodeB);
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            _cts.Cancel();
            loadingTask?.Dispose();
        }

        private readonly CancellationTokenSource _cts = new();
        private CancellationTokenSource? _tmpCTS = null;
        private event Action? LoadCompleted;
        private Task? loadingTask;
        private NodeBase[]? loadedNodes = null;

        private async Task LoadAllNodes(CancellationToken token)
        {
            Trace.WriteLine("Loading all nodes");
            if (token.IsCancellationRequested) return;

            Nodes.Clear();

            var task = _packVM.QuestPack.GetNodesAsync(_quest.Tag);
            loadedNodes = null;

            loadedNodes = await task;

            if (task.IsCanceled || loadedNodes == null || token.IsCancellationRequested)
                return;

            if (token.IsCancellationRequested)
                loadedNodes = null;

            LoadCompleted?.Invoke();
        }
        public void Select()
        {
            LoadCompleted -= OnLoadCompleted;
            LoadCompleted += OnLoadCompleted;
            _tmpCTS?.Cancel();
            _tmpCTS?.Dispose();
            _tmpCTS = new();
            loadingTask = LoadAllNodes(_tmpCTS!.Token);

        }

        void OnLoadCompleted()
        {
            LoadCompleted -= OnLoadCompleted;
            _tmpCTS?.Cancel();
            _tmpCTS?.Dispose();
            _tmpCTS = null;
            loadingTask?.Dispose();
            loadingTask = null;

            if (loadedNodes != null)
                Nodes = new(loadedNodes.Select(n => NodeVM.SelectViewModel(n)));
            Trace.WriteLine("Loaded all nodes");
        }

        public void ClearSelection()
        {
            _tmpCTS?.Cancel();
            _tmpCTS?.Dispose();
            _tmpCTS = null;
            loadingTask?.Dispose();
            loadingTask = null;
            Nodes.Clear();
        }
    }
}