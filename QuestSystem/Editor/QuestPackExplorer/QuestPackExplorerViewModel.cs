using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Input;
using QuestEditor.Shared;

namespace QuestEditor.QuestPackExplorer
{
    public sealed class QuestPackExplorerViewModel : ViewModelBase
    {
        private readonly QuestPackExplorerService _explorerService;
        private readonly IDialogService _dialogService;

        public QuestPackExplorerViewModel(QuestPackExplorerService explorerService, IDialogService dialogService)
        {
            _explorerService = explorerService;
            _dialogService = dialogService;

            QuestTags = new ObservableCollection<string>(_explorerService.QuestTags);

            CreatePackFileCommand = new RelayCommand(_ => { CreateNewPack(); RefreshFromModel(); });
            SelectPackFileCommand = new RelayCommand(_ => { OpenExistingPack(); RefreshFromModel(); });
            SavePackFileCommand = new RelayCommand(_ => { _explorerService.SaveCurrentPack(); RefreshFromModel(); }, _ => IsPackFileSelected);
            SaveAsPackFileCommand = new RelayCommand(_ => { SaveCurrentPackAs(); RefreshFromModel(); }, _ => IsPackFileSelected);
           
            ExitCommand = new RelayCommand(_ => Environment.Exit(0));
           
            AddQuestCommand = new RelayCommand(AddNewQuest, _=> !string.IsNullOrEmpty(NewQuestTag));
            RemoveQuestCommand = new RelayCommand(RemoveSelectedQuest, _=> IsQuestSelected);
        }
        
        public ObservableCollection<string> QuestTags { get; }

        public string SelectedPackFileName => _explorerService.PackName ?? "(No QuestPack Selected)";
        public Brush QuestPackNameColor => IsPackFileSelected ? Brushes.Black : Brushes.Gray;

        public string? SelectedQuestTag 
        {
            get => _explorerService.SelectedQuestTag;
            set
            {
                _explorerService.SelectQuest(value);

                OnPropertyChanged(nameof(SelectedQuestTag));
                OnPropertyChanged(nameof(SelectedQuestTagDisplay));
                OnPropertyChanged(nameof(QuestNameColor));
                OnPropertyChanged(nameof(IsQuestSelected));

                (RemoveQuestCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }    

        public string SelectedQuestTagDisplay => SelectedQuestTag ?? "(No Quest selected)";


        public Brush QuestNameColor => string.IsNullOrEmpty(SelectedQuestTag) ? Brushes.Black : Brushes.Gray;
        
        public bool IsPackFileSelected => !string.IsNullOrEmpty(_explorerService.PackName);
        public bool IsQuestSelected => !string.IsNullOrEmpty(_explorerService.SelectedQuestTag);

        string newQuestTag = string.Empty;
        public string NewQuestTag
        {
            get => newQuestTag;
            set
            {
                newQuestTag = IsPackFileSelected ? value : string.Empty;
                OnPropertyChanged(nameof(NewQuestTag));
                (AddQuestCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void CreateNewPack(object? _ = null)
        {
            var defName = _explorerService.PackName ?? "NewQuestPack";
            var filePath = _dialogService.ShowSaveFileDialog(defName);
            _explorerService.CreatePackFile(filePath);
            RefreshFromModel();
        }

        private void OpenExistingPack(object? _ = null)
        {
            var filePath = _dialogService.ShowOpenFileDialog();
            _explorerService.SelectPackFile(filePath);
            RefreshFromModel();
        }

        private void SaveCurrentPackAs()
        {
            var defName = _explorerService.PackName ?? "NewQuestPack";
            var filePath = _dialogService.ShowSaveFileDialog(defName);
            _explorerService.SaveCurrentPackAs(filePath);
            RefreshFromModel();
        }

        private void AddNewQuest(object? _ = null){
            if(_explorerService.AddQuest(NewQuestTag))
                RefreshFromModel();
        }

        private void RemoveSelectedQuest(object? _ = null){
            if(_explorerService.RemoveQuest(SelectedQuestTag))
                RefreshFromModel();
        }

        public ICommand CreatePackFileCommand { get; }
        public ICommand SelectPackFileCommand { get; }
        public ICommand SavePackFileCommand { get; }
        public ICommand SaveAsPackFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AddQuestCommand { get; }
        public ICommand RemoveQuestCommand { get; }



        private void RefreshFromModel()
        {
            QuestTags.Clear();
            if (_explorerService.QuestTags != null)
            {
                foreach (var t in _explorerService.QuestTags) QuestTags.Add(t);
            }
            OnPropertyChanged(nameof(SelectedPackFileName));
            OnPropertyChanged(nameof(QuestPackNameColor));
            OnPropertyChanged(nameof(IsPackFileSelected));
            OnPropertyChanged(nameof(QuestNameColor));
            OnPropertyChanged(nameof(SelectedQuestTagDisplay));
            OnPropertyChanged(nameof(IsQuestSelected));


            (AddQuestCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveQuestCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SavePackFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveAsPackFileCommand as RelayCommand)?.RaiseCanExecuteChanged();

        }

    }
}
