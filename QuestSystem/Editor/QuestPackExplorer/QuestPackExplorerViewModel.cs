using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuestEditor.QuestPackExplorer
{
    public sealed class QuestPackExplorerViewModel : ViewModelBase
    {
        public QuestPackExplorerViewModel()
        {
            QuestTags = new ObservableCollection<string>(QuestPackExplorerModel.QuestTags ?? []);

            CreatePackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.CreatePackFile(); RefreshFromModel(); });
            SelectPackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.SelectPackFile(); RefreshFromModel(); });
            SavePackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.SaveCurrentPack(); RefreshFromModel(); }, _ => IsPackFileSelected);
            SaveAsPackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.SaveCurrentPackAs(); RefreshFromModel(); }, _ => IsPackFileSelected);
           
            ExitCommand = new RelayCommand(_ => Environment.Exit(0));
           
            AddQuestCommand = new AsyncRelayCommand<string>(AddQuestAsync, _=>CanAddNewQuestTag);
            RemoveQuestCommand = new RelayCommand(param => RemoveQuest(param as string));
        }

        public ObservableCollection<string> QuestTags { get; }

        public Brush QuestPackNameColor => IsPackFileSelected ? Brushes.Black : Brushes.Gray;
        public Brush QuestNameColor => string.IsNullOrEmpty(SelectedQuestTag) ? Brushes.Black : Brushes.Gray;
        public string SelectedPackFileName => QuestPackExplorerModel.SelectedPackFileName ?? "(No QuestPack Selected)";
        public bool IsPackFileSelected => !string.IsNullOrEmpty(QuestPackExplorerModel.SelectedPackFileName);
        public int SelectedQuest => QuestPackExplorerModel.CurrentQuestTag != null ? QuestTags.IndexOf(QuestPackExplorerModel.CurrentQuestTag) : -1;
        public string SelectedQuestTag 
        {
            get => QuestPackExplorerModel.CurrentQuestTag != null ? QuestPackExplorerModel.CurrentQuestTag : "(No Quest selected)";
            set
            {
                // if(value == QuestPackExplorerModel.CurrentQuestTag) return;

                // QuestPackExplorerModel.SelectQuestAsync(value)
                //     .ConfigureAwait(false)
                //     .GetAwaiter()
                //     .GetResult();
                //     // tmp: block UI until quest is loaded
                try
                {
                    QuestPackExplorerModel.SelectQuestAsync(value)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR in setter: " + ex);
                    throw; // or handle
                }


                OnPropertyChanged(nameof(SelectedQuestTag));
                OnPropertyChanged(nameof(QuestNameColor));
                OnRefreshFromModel?.Invoke();

                Console.WriteLine("Quest selected in VM");
            }
        }
        string newQuestTag = string.Empty;
        public string NewQuestTag
        {
            get => IsPackFileSelected ? newQuestTag : string.Empty;
            set
            {
                if(value == newQuestTag) return;
                newQuestTag = value;
                OnPropertyChanged(nameof(NewQuestTag));
            }
        }
        public bool CanAddNewQuestTag => !string.IsNullOrEmpty(NewQuestTag);

        public ICommand CreatePackFileCommand { get; }
        public ICommand SelectPackFileCommand { get; }
        public ICommand SavePackFileCommand { get; }
        public ICommand SaveAsPackFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AddQuestCommand { get; }
        public ICommand RemoveQuestCommand { get; }

        private async Task AddQuestAsync(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            if(await QuestPackExplorerModel.AddQuest(tag))
                RefreshFromModel();
        }

        private void RemoveQuest(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            if (QuestPackExplorerModel.RemoveQuest(tag))
                RefreshFromModel();
            
        }

        private void RefreshFromModel()
        {
            QuestTags.Clear();
            if (QuestPackExplorerModel.QuestTags != null)
            {
                foreach (var t in QuestPackExplorerModel.QuestTags) QuestTags.Add(t);
            }
            OnPropertyChanged(nameof(SelectedPackFileName));
            OnPropertyChanged(nameof(QuestPackNameColor));
            OnPropertyChanged(nameof(IsPackFileSelected));
            OnPropertyChanged(nameof(SelectedQuestTag));
            OnPropertyChanged(nameof(QuestNameColor));

            OnRefreshFromModel?.Invoke();
        }

        public event Action OnRefreshFromModel;
    }
}
