using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using QuestEditor.Shared;
using QuestSystem;

namespace QuestEditor.Explorer
{
    public sealed class QuestPackVM : StatefulViewModelBase, IDisposable, ISelectable, IClickableViewModel
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

        private CancellationTokenSource? _tmpCTS = null;
        private Quest[]? loadedQuests = null;
        private event Action? LoadCompleted;
        private Task? loadingTask = null;

        public ObservableCollection<QuestVM> Quests { get; set; } = [];

        private QuestPackVM(string filePath)
        {
            _filePath = filePath;
            _packName = Path.GetFileNameWithoutExtension(_filePath);
            originalQuestPack = EditorQuestPack.OpenRead(File.OpenRead(filePath),globalToken:_cts.Token);
            ClickedCommand = new RelayCommand(OnClicked, _ => true);
        }

        private async Task LoadAllQuests(CancellationToken token)
        {
            Trace.WriteLine("Loading all quests");
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
        }

        public ICommand ClickedCommand { get; }
        void OnClicked(object? _)
        {
            if (!IsSelected) 
                Select();
            else if (System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl))
                ClearSelection();
        }

        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }


        private bool _isSelected = false;

        private string _packName;
        



        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            _cts.Cancel();
            loadingTask?.Dispose();
            originalQuestPack?.Dispose();
            temporaryQuestPack?.Dispose();
        }

        public void Select()
        {
            LoadCompleted -= OnLoadCompleted;
            LoadCompleted += OnLoadCompleted;
            _tmpCTS?.Cancel();
            _tmpCTS?.Dispose();
            _tmpCTS = new();
            loadingTask = LoadAllQuests(_tmpCTS!.Token);
            IsSelected = true;
        }

        void OnLoadCompleted()
        {
            LoadCompleted -= OnLoadCompleted;
            _tmpCTS?.Cancel();
            _tmpCTS?.Dispose();
            _tmpCTS = null;
            loadingTask?.Dispose();
            loadingTask = null;

            if(loadedQuests != null)
                Quests = new(loadedQuests.Select(q=>new QuestVM(q, this)));
            Trace.WriteLine("Loaded all quests");
        }

        public void ClearSelection()
        {
            _tmpCTS?.Cancel();
            _tmpCTS?.Dispose();
            _tmpCTS = null;
            loadingTask?.Dispose();
            loadingTask = null;
            Quests.Clear();
            IsSelected = false;
        }
    }
}