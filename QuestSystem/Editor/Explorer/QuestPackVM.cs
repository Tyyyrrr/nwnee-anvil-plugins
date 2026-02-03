using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using QuestEditor.Shared;
using QuestSystem;

namespace QuestEditor.Explorer
{
    public sealed class QuestPackVM : StatefulViewModelBase, ISelectable
    {


        private readonly PackManager _manager;
        public ObservableCollection<QuestVM> Quests { get; set; } = [];
        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => Quests;


        public QuestPackVM(string filePath, ExplorerVM explorer, PackManager manager) : base(explorer)
        {
            _manager = manager;

            manager.QuestsLoadCompleted += OnLoadCompleted;

            _packName = Path.GetFileNameWithoutExtension(filePath);

            AddQuestCommand = new RelayCommand(AddQuest, _ => true);

            manager.LoadAllQuests();
        }

        public string PackName
        {
            get => _packName;
            private set => SetProperty(ref _packName, value);
        } private string _packName;

        public override void RefreshIsDirty()
        {
            base.RefreshIsDirty();
            RaisePropertyChanged(nameof(DisplayFontStyle));
            RaisePropertyChanged(nameof(DisplayFontWeight));
            RaisePropertyChanged(nameof(DisplayText));
        }

        public override bool IsDirty => base.IsDirty || Quests.Any(n => n.IsDirty);

        public string DisplayText => IsDirty ? $"{PackName}*" : PackName;
        public FontWeight DisplayFontWeight => IsDirty ? FontWeights.Bold : FontWeights.Regular;
        public FontStyle DisplayFontStyle => IsDirty ? FontStyles.Italic : FontStyles.Normal;



        public ICommand AddQuestCommand { get; }

        private sealed class AddQuestOperation(Quest model, QuestPackVM pack) : UndoableOperation(pack)
        {
            private readonly Quest _model = model;
            private QuestVM? viewModel;

            protected override void ProtectedDo()
            {
                pack._manager.WriteQuest(_model);
                viewModel = new(_model, pack, pack._manager);
                pack.Quests.Add(viewModel!);
                viewModel.Renamed += pack.OnQuestRenamed;
            }
            protected override void ProtectedUndo()
            {
                pack.Quests.Remove(viewModel!);
                viewModel!.Unsubscribe();
                viewModel.Renamed -= pack.OnQuestRenamed;
                pack._manager.RemoveQuest(_model.Tag);
            }
            protected override void ProtectedRedo()
            {
                pack.Quests.Add(viewModel!);
                viewModel!.Subscribe();
                viewModel.Renamed += pack.OnQuestRenamed;
                pack._manager.WriteQuest(_model);
            }
        }

        private sealed class RemoveQuestOperation(QuestVM questVM, QuestPackVM pack) : UndoableOperation(pack)
        {
            private readonly QuestVM _viewModel = questVM;
            protected override void ProtectedDo() => ProtectedRedo();
            protected override void ProtectedUndo()
            {
                pack.Quests.Add(_viewModel);
                _viewModel.Subscribe();
                _viewModel.Renamed += pack.OnQuestRenamed;
                pack._manager.WriteQuest(_viewModel.Model);
            }
            protected override void ProtectedRedo()
            {
                if (_viewModel.IsSelected)
                {
                    _viewModel.ClearSelection();
                }
                pack.Quests.Remove(_viewModel);
                _viewModel.Unsubscribe();
                _viewModel.Renamed -= pack.OnQuestRenamed;
                pack._manager.RemoveQuest(_viewModel.Model.Tag);
            }
        }

        private sealed class RenameQuestOperation(QuestVM questVM, QuestPackVM pack, string oldName, string newName) : UndoableOperation(pack)
        {
            protected override void ProtectedDo()
            {
                questVM.QuestTag = newName;
                pack._manager.RemoveQuest(oldName);
                pack._manager.WriteQuest(questVM.Model);
                questVM.RecursiveApply();
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                questVM.QuestTag = oldName;
                pack._manager.RemoveQuest(newName);
                pack._manager.WriteQuest(questVM.Model);
                questVM.RecursiveApply();
            }
        }

        void AddQuest(object? _)
        {
            string newTag = "New Quest";
            int count = 1;
            while (Quests.Any(q => q.QuestTag == newTag))
                newTag = $"New Quest ({count++})";

            var quest = new Quest() { Tag = newTag, Name = "[text displayed in player's journal]" };
            Trace.WriteLine("Adding new quest");
            PushOperation(new AddQuestOperation(quest, this));
        }

        public void DeleteQuest(object? parameter)
        {
            if (parameter is not QuestVM questVM) throw new InvalidOperationException("Parameter is not a QuestVM");
            if (!Quests.Remove(questVM)) throw new InvalidOperationException("QuestVM <-> QuestPackVM mismatch");
            PushOperation(new RemoveQuestOperation(questVM, this));
        }

        public void ReloadAllQuests()
        {
            foreach (var quest in Quests)
            {
                quest.Unsubscribe();
                quest.Renamed -= OnQuestRenamed;
            }
            Quests.Clear();
            _manager.LoadAllQuests();
        }

        public void ResetTemporaryFile() => _manager.DiscardChanges();

        public void ReloadSingleQuest(QuestVM questVM)
        {
            questVM.Nodes.Clear();
            _manager.LoadAllNodes(questVM.Model);
        }


        public bool IsSelected { 
            get => _isSelected;
            private set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public void Select() => IsSelected = true;

        void OnLoadCompleted(Quest[]? quests)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Trace.WriteLine($"{PackName}: On quests load completed");
                if (quests == null)
                {
                    Trace.WriteLine("... but operation has failed.");
                    return;
                }

                string str = "";
                foreach (var quest in quests)
                    str += $"\n{quest}";
                
                Trace.WriteLine("Loaded quests:" + str);

                foreach (var quest in Quests) quest.Unsubscribe();
                Quests.Clear();
                foreach (var quest in quests)
                {
                    var questVM = new QuestVM(quest, this, _manager);
                    Quests.Add(questVM);
                    questVM.Renamed += OnQuestRenamed;
                }
            });
        }

        void OnQuestRenamed(QuestVM quest, string oldName, string newName)
        {
            Trace.WriteLine("Quest rename end");
            if (string.IsNullOrWhiteSpace(newName) || newName == string.Empty || Quests.Any(q => q.QuestTag == newName))
                return;

            PushOperation(new RenameQuestOperation(quest, this, oldName, newName));
        }


        public void ClearSelection()
        {
            IsSelected = false;
        }

        protected override void Apply()
        {
            _manager.ApplyChanges();
        }

    }
}