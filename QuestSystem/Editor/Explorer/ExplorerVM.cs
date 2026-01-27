using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace QuestEditor.Explorer
{
    public sealed class ExplorerVM : StatefulViewModelBase
    {
        public ICommand SelectableItemClickedCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }

        readonly ICreateFileDialog _createFileDialog;
        readonly IOpenFilesDialog _openFilesDialog;

        public QuestVM? SelectedQuest
        {
            get => _selectedQuest;
            private set => SetProperty(ref _selectedQuest, value);
        } private QuestVM? _selectedQuest = null;

        private List<ISelectable> _selectedItems = [];
        private Dictionary<QuestPackVM, List<QuestVM>> _selectedQuests = [];
        private Dictionary<QuestVM, List<NodeVM>> _selectedNodes = [];

        public ObservableCollection<QuestPackVM> QuestPacks { get; } = [];

        public ExplorerVM()
        {
            NewCommand = new RelayCommand(CreateNewPack, _ => true);
            OpenCommand = new RelayCommand(OpenPacks, _ => true);
            SelectableItemClickedCommand = new RelayCommand(OnSelectableItemClicked, _ => true);
            ClearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => true);

            var fd = new FileDialog();

            _openFilesDialog = fd;
            _createFileDialog = fd;
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
            pack.Select();
        }
        private void OnQuestClicked(QuestVM quest)
        {
            //todo: set CURRENT quest property for Graph editor

            foreach (var qp in QuestPacks)
            {
                if (qp.Quests.Contains(quest))
                {
                    OnPackClicked(qp);
                    quest.Select();
                    _selectedItems.Add(quest);
                    _selectedQuests.Add(qp, [quest]);
                    SelectedQuest = quest;
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
        }
        //////

        private sealed class CreateNewPackOperation : IUndoable
        {
            private readonly string _packFileName;
            private QuestPackVM? _pack;
            private readonly ExplorerVM _origin;
            public void Redo()
            {
                _pack = QuestPackVM.New(_packFileName);
                _origin.QuestPacks.Add(_pack);
            }

            public void Undo()
            {
                _origin.QuestPacks.Remove(_pack!);
                _pack?.Dispose();
                File.Delete(_packFileName);
            }

            public CreateNewPackOperation(ExplorerVM origin, string fileName)
            {
                _packFileName = fileName;
                _origin = origin;
                Redo();
            }
        }

        void CreateNewPack(object? _) // creation is undoable
        {
            string fname = _createFileDialog.GetFileNameFromUser();

            if (fname == string.Empty) return;

            PushOperation(new CreateNewPackOperation(this, fname));
        }

        void OpenPacks(object? _) // opening is not undoable
        {
            foreach (var qp in QuestPacks)
                qp.Dispose();

            QuestPacks.Clear();

            string[] fnames = _openFilesDialog.GetFileNamesFromUser();

            if (fnames.Length == 0) return;

            foreach(var fname in fnames)
            {
                QuestPacks.Add(QuestPackVM.Edit(fname));
            }
        }
    }
}