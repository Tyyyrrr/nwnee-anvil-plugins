using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using QuestEditor.Shared;
using QuestSystem;

namespace QuestEditor.Explorer
{
    public sealed class QuestPackVM : StatefulViewModelBase, IDisposable, ISelectable
    {
        public static QuestPackVM New(string filePath)
        {
            bool overwriteExisting = File.Exists(filePath);
            var fs = overwriteExisting ? 
                File.Open(filePath, FileMode.Truncate, FileAccess.ReadWrite) :
                File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
            EditorQuestPack.OpenWrite(fs).Dispose();

            return Edit(filePath);
        }

        public static QuestPackVM Edit(string filePath) => new(filePath);


        private readonly string _filePath;

        private EditorQuestPack originalQuestPack;
        private EditorQuestPack? temporaryQuestPack = null;

        public EditorQuestPack QuestPack => temporaryQuestPack ?? originalQuestPack;

        private readonly CancellationTokenSource _cts = new();

        private Quest[]? loadedQuests = null;
        private event Action? LoadCompleted;
        private Task? loadingTask = null;

        public ObservableCollection<QuestVM> Quests { get; set; } = [];

        private QuestPackVM(string filePath)
        {
            _filePath = filePath;
            _packName = Path.GetFileNameWithoutExtension(_filePath);
            originalQuestPack = EditorQuestPack.OpenRead(File.OpenRead(filePath),globalToken:_cts.Token);
            AddQuestCommand = new RelayCommand(AddQuest, _ => true);

            LoadCompleted += OnLoadCompleted;
            loadingTask = LoadAllQuests(_cts.Token);
        }

        private async Task LoadAllQuests(CancellationToken token)
        {
            if(token.IsCancellationRequested) return;

            Quests.Clear();

            var task = originalQuestPack.GetQuestsAsync();
            loadedQuests = null;

            loadedQuests = await task;

            if (task.IsCanceled || loadedQuests == null || token.IsCancellationRequested) 
                return;

            if (token.IsCancellationRequested)
                loadedQuests = null;

            LoadCompleted?.Invoke();
        }

        public string PackName
        {
            get => _packName;
            private set => SetProperty(ref _packName, value);
        } private string _packName;


        public ICommand AddQuestCommand { get; }
        void AddQuest(object? _)
        {
            string newTag = "New Quest";
            int count = 1;
            while (Quests.Any(q => q.QuestTag == newTag))
                newTag = $"New Quest ({count++})";

            var quest = new Quest() { Tag = newTag, Name = "[text displayed in player's journal]" };
            var vm = new QuestVM(quest, this);
            Quests.Add(vm);
            RaisePropertyChanged(nameof(Quests));
        }


        public bool IsSelected { 
            get => _isSelected;
            private set => SetProperty(ref _isSelected, value);
        } private bool _isSelected = false;


        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            _cts.Cancel();
            _cts.Dispose();
            loadingTask?.Dispose();
            originalQuestPack?.Dispose();
            temporaryQuestPack?.Dispose();
        }

        public void Select()
        {
            IsSelected = true;
        }

        void OnLoadCompleted()
        {
            LoadCompleted -= OnLoadCompleted;
            loadingTask?.Dispose();
            loadingTask = null;

            if(loadedQuests != null)
                Quests = new(loadedQuests.Select(q=>new QuestVM(q, this)));

            RaisePropertyChanged(nameof(Quests));
        }

        public void ClearSelection()
        {
            IsSelected = false;
        }
    }
}