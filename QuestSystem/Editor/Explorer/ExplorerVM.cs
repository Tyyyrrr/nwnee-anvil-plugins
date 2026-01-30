using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace QuestEditor.Explorer
{
    public sealed class ExplorerVM : StatefulViewModelBase, IDisposable, IAsyncDisposable
    {
        public event Action<QuestVM?>? QuestSelected;

        public ICommand SelectableItemClickedCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }

        readonly ICreateFileDialog _createFileDialog;
        readonly IOpenFilesDialog _openFilesDialog;

        public QuestVM? SelectedQuest
        {
            get => _selectedQuest;
            set
            {
                if(SetProperty(ref _selectedQuest, value))
                    QuestSelected?.Invoke(value);
            }
        } private QuestVM? _selectedQuest = null;

        private List<ISelectable> _selectedItems = [];
        private Dictionary<QuestPackVM, List<QuestVM>> _selectedQuests = [];
        private Dictionary<QuestVM, List<NodeVM>> _selectedNodes = [];

        private List<PackManager> _packManagers = new();
        public ObservableCollection<QuestPackVM> QuestPacks { get; } = [];

        public ExplorerVM()
        {
            NewCommand = new RelayCommand(CreateNewPack, _ => true);
            OpenCommand = new RelayCommand(OpenPacks, _ => true);

            SelectableItemClickedCommand = new RelayCommand(OnSelectableItemClicked, _ => true);
            ClearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => true);
            SaveCommand = new RelayCommand(_ =>
            {
                foreach (var qp in QuestPacks.Where(qp=>qp.IsDirty))
                {
                    qp.RecursiveApply();
                }
                RefreshIsDirty();
            },
            _=>QuestPacks.Any(qp=>qp.IsDirty));
            var fd = new FileDialog();

            _openFilesDialog = fd;
            _createFileDialog = fd;
        }


        public override bool IsDirty => base.IsDirty || QuestPacks.Any(n => n.IsDirty);

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => QuestPacks;



        public void ReopenAllPacks()
        {
            throw new NotImplementedException();
        }

        void OnSelectableItemClicked(object? ctx)
        {
            switch (ctx)
            {
                case QuestPackVM pack:
                    OnPackClicked(pack);
                    break;

                case QuestVM quest:
                    OnQuestClicked(quest);
                    break;

                case NodeVM node:
                    OnNodeClicked(node);
                    break;

                default:
                    ClearSelection();
                    break;
            }
        }

        public void ClearSelection()
        {
            Trace.WriteLine(this.GetHashCode().ToString() + "ClearingSelection. Selected items: " + _selectedItems.Count);
            SelectedQuest = null;
            foreach (var selectable in _selectedItems)
                selectable.ClearSelection();
            _selectedItems.Clear();
            _selectedQuests.Clear();
            _selectedNodes.Clear();
        }

        //////
        // TODO: suppord multiselection with shift and ctrl
        private void OnPackClicked(QuestPackVM pack)
        {
            ClearSelection();
            _selectedItems.Add(pack);
            if (pack != (SelectedQuest?.PackVM ?? null))
                SelectedQuest = null;
            pack.Select();
        }
        private void OnQuestClicked(QuestVM quest)
        {
            foreach (var qp in QuestPacks)
            {
                if (qp.Quests.Contains(quest))
                {
                    OnPackClicked(qp);
                    quest.Select();
                    _selectedItems.Add(quest);
                    _selectedQuests.Add(qp, [quest]);
                    SelectedQuest = quest;
                    QuestSelected?.Invoke(quest);
                    return;
                }
            }

            throw new InvalidOperationException("Pack not found for selected quest");
        }
        private void OnNodeClicked(NodeVM node)
        {
            if (SelectedQuest == null || !SelectedQuest.Nodes.Contains(node))
                throw new InvalidOperationException("Active quest not found for selected node");

            foreach (var sn in _selectedNodes[SelectedQuest])
            {
                sn.ClearSelection();
                _selectedItems.Remove(sn);
            }
            _selectedNodes[SelectedQuest] = [node];
            node.Select();
        }
        //////

        void CreateNewPack(object? _)
        {
            string fname = _createFileDialog.GetFileNameFromUser();

            if (fname == string.Empty) return;

            bool overwriteExisting = File.Exists(fname);
            var fs = overwriteExisting ?
                File.Open(fname, FileMode.Truncate, FileAccess.ReadWrite) :
                File.Open(fname, FileMode.CreateNew, FileAccess.ReadWrite);
            EditorQuestPack.OpenWrite(fs).Dispose();

            var manager = new PackManager(fname);
            _packManagers.Add(manager);
            var packVM = new QuestPackVM(fname, this, manager);
            QuestPacks.Add(packVM);
        }

        void OpenPacks(object? _)
        {
            QuestPacks.Clear();

            string[] fnames = _openFilesDialog.GetFileNamesFromUser();

            if (fnames.Length == 0) return;

            foreach(var fname in fnames)
            {
                var manager = new PackManager(fname);
                _packManagers.Add(manager);
                var pack = new QuestPackVM(fname, this, manager);
                QuestPacks.Add(pack);
            }
        }

        public void Dispose()
        {
            QuestPacks.Clear();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var manager in _packManagers)
                await manager.DisposeAsync();

            _packManagers.Clear();
        }
    }
}