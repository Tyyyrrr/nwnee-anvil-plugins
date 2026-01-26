using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Collections;

namespace QuestEditor.Explorer
{
    public sealed class ExplorerVM : StatefulViewModelBase
    {
        public ICommand SelectableItemClickedCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }

        readonly ICreateFileDialog _createFileDialog;
        readonly IOpenFilesDialog _openFilesDialog;

        public IList SelectedItems => _selectedItems; 
        private readonly ObservableCollection<object> _selectedItems = new();

        public ObservableCollection<QuestPackVM> SelectedPacks { get; } = [];
        public ObservableCollection<QuestVM> SelectedQuests { get; } = [];
        public ObservableCollection<NodeVM> SelectedNodes { get; } = [];

        public ObservableCollection<QuestPackVM> QuestPacks { get; } = [];

        private void UpdateSelection()
        {
            SelectedPacks.Clear();
            SelectedQuests.Clear();
            SelectedNodes.Clear();

            foreach (var item in SelectedItems)
            {
                switch (item)
                {
                    case QuestPackVM pack:
                        SelectedPacks.Add(pack);
                        break;

                    case QuestVM quest:
                        SelectedQuests.Add(quest);
                        break;

                    case NodeVM node:
                        SelectedNodes.Add(node);
                        break;
                }
            }
        }

        public ExplorerVM()
        {
            _selectedItems.CollectionChanged += (_, __) => UpdateSelection();
            NewCommand = new RelayCommand(CreateNewPack, _ => true);
            OpenCommand = new RelayCommand(OpenPacks, _ => true);
            SelectableItemClickedCommand = new RelayCommand(OnSelectableItemClicked, _ => true);

            var fd = new FileDialog();

            _openFilesDialog = fd;
            _createFileDialog = fd;
        }

        void OnSelectableItemClicked(object? ctx)
        {
            if (ctx is QuestPackVM pack)
            {
                Trace.WriteLine("Selected PACK!");
            }
            else if (ctx is QuestVM quest)
            {
                Trace.WriteLine("Selected QUEST!");
            }
            else if (ctx is NodeVM node)
            {
                Trace.WriteLine("Selected NODE!");
            }
            else throw new InvalidOperationException("Selected invalid item");
        }

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