using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Input;
using QuestEditor.Shared;
using QuestSystem;

namespace QuestEditor.QuestPackExplorer
{
    public sealed class QuestPackExplorerViewModel : ViewModelBase
    {
        private readonly QuestPackExplorerService _explorerService;
        private readonly IFileDialogService _fileDialogService;

        public QuestPackExplorerViewModel(QuestPackExplorerService explorerService, IFileDialogService dialogService)
        {
            _explorerService = explorerService;
            _fileDialogService = dialogService;

            QuestTags = new ObservableCollection<string>(_explorerService.QuestTags);

            CreatePackFileCommand = new RelayCommand(_ => { CreateNewPack(); RefreshFromModel(); });
            SelectPackFileCommand = new RelayCommand(_ => { OpenExistingPack(); RefreshFromModel(); });
            SavePackFileCommand = new RelayCommand(_ => { _explorerService.SaveCurrentPack(); RefreshFromModel(); }, _ => IsPackFileSelected);
            SaveAsPackFileCommand = new RelayCommand(_ => { SaveCurrentPackAs(); RefreshFromModel(); }, _ => IsPackFileSelected);
           
            ExitCommand = new RelayCommand(_ => Environment.Exit(0));
           
            AddQuestCommand = new RelayCommand(AddNewQuest, _ => CanAddNewQuest);
            RemoveQuestCommand = new RelayCommand(RemoveSelectedQuest, _ => IsQuestSelected);
        }
        
        public ObservableCollection<string> QuestTags { get; }

        public string SelectedPackDisplayName => _explorerService.PackName+QuestPack.FileExtension ?? "(No QuestPack Selected)";
        public Brush QuestPackNameColor => IsPackFileSelected ? Brushes.Black : Brushes.Gray;

        public string? SelectedQuestTag 
        {
            get => _selectedQuestTag;
            set
            {
                try{
                Console.WriteLine("Selecting quest: " + value);

                if(_selectedQuestTag == value)
                { 
                    Console.WriteLine("Selected tag == "+value+". Skipping.");
                    return;
                }

                if(IsQuestSelected && _selectedQuestTag != value)
                {
                    _explorerService.ApplyChanges(_selectedQuestTag!);
                }

                if (!_explorerService.SelectQuest(value))
                {
                    Console.WriteLine("Failed to select quest " + value  + " in explorer");
                    return;
                }

                _selectedQuestTag = value;
                Console.WriteLine("Quest selected: " + value);

                OnPropertyChanged(nameof(SelectedQuestTag));
                OnPropertyChanged(nameof(SelectedQuestTagDisplay));
                OnPropertyChanged(nameof(QuestNameColor));
                OnPropertyChanged(nameof(IsQuestSelected));

                (RemoveQuestCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                catch(Exception ex){ Console.WriteLine("EXCEPTION:"+ex.Message+"\n"+ex.StackTrace);throw;}
            }
        } private string? _selectedQuestTag = null;  

        public string SelectedQuestTagDisplay => SelectedQuestTag ?? "(No Quest selected)";


        public Brush QuestNameColor => string.IsNullOrEmpty(SelectedQuestTag) ? Brushes.Black : Brushes.Gray;
        
        public bool IsPackFileSelected => !string.IsNullOrEmpty(_explorerService.PackName);
        public bool IsQuestSelected => !string.IsNullOrEmpty(_selectedQuestTag);
            
        

        string newQuestTag = string.Empty;
        public string NewQuestTag
        {
            get => newQuestTag;
            set
            {
                newQuestTag = value;
                OnPropertyChanged(nameof(NewQuestTag));
                OnPropertyChanged(nameof(CanAddNewQuest));
                (AddQuestCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool CanAddNewQuest => IsPackFileSelected && !string.IsNullOrEmpty(NewQuestTag) && !NewQuestTag.Contains('/'); // todo filter more invalid chars

        private void CreateNewPack(object? _ = null)
        {
            if(IsQuestSelected) _explorerService.ApplyChanges(_selectedQuestTag!);
            var defName = _explorerService.PackName ?? "NewQuestPack";
            var filePath = _fileDialogService.ShowSaveFileDialog(defName);
            _explorerService.CreatePackFile(filePath);
            RefreshFromModel();
        }

        private void OpenExistingPack(object? _ = null)
        {
            if(IsQuestSelected) _explorerService.ApplyChanges(_selectedQuestTag!);
            var filePath = _fileDialogService.ShowOpenFileDialog();
            _explorerService.SelectPackFile(filePath);
            RefreshFromModel();
        }

        private void SaveCurrentPackAs()
        {
            if(IsQuestSelected) _explorerService.ApplyChanges(_selectedQuestTag!);
            var defName = _explorerService.PackName ?? "NewQuestPack";
            var filePath = _fileDialogService.ShowSaveFileDialog(defName);
            _explorerService.SaveCurrentPackAs(filePath);
            RefreshFromModel();
        }

        private void AddNewQuest(object? _ = null){
            if(IsQuestSelected) _explorerService.ApplyChanges(_selectedQuestTag!);
            if(_explorerService.AddQuest(NewQuestTag))
                RefreshFromModel();
        }

        private void RemoveSelectedQuest(object? questTag){
            if(questTag is not string str) throw new InvalidOperationException("Invalid command parameter binding");
            if(IsQuestSelected) _explorerService.ApplyChanges(_selectedQuestTag!);
            if(_explorerService.RemoveQuest(str))
                RefreshFromModel();
            else Console.WriteLine("Failed to remove quest " + str);
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
            OnPropertyChanged(nameof(SelectedPackDisplayName));
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
