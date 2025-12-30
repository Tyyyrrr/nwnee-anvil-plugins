using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuestEditor.QuestPackExplorer
{
    public sealed class QuestPackExplorerViewModel : INotifyPropertyChanged
    {
        public QuestPackExplorerViewModel()
        {
            QuestTags = new ObservableCollection<string>(QuestPackExplorerModel.QuestTags ?? []);

            CreatePackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.CreatePackFile(); RefreshFromModel(); });
            SelectPackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.SelectPackFile(); RefreshFromModel(); });
            SavePackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.SaveCurrentPack(); RefreshFromModel(); }, _ => IsPackFileSelected);
            SaveAsPackFileCommand = new RelayCommand(_ => { QuestPackExplorerModel.SaveCurrentPackAs(); RefreshFromModel(); }, _ => IsPackFileSelected);
           
            ExitCommand = new RelayCommand(_ => Environment.Exit(0));
           
            AddQuestCommand = new AsyncRelayCommand<string>(AddQuestAsync);
            RemoveQuestCommand = new RelayCommand(param => RemoveQuest(param as string));
        }

        public ObservableCollection<string> QuestTags { get; }

        public Brush QuestPackNameColor => IsPackFileSelected ? Brushes.Black : Brushes.Gray;
        public string SelectedPackFileName => QuestPackExplorerModel.SelectedPackFileName ?? "(No QuestPack Selected)";
        public bool IsPackFileSelected => !string.IsNullOrEmpty(QuestPackExplorerModel.SelectedPackFileName);
        public int SelectedQuest => QuestPackExplorerModel.CurrentQuestTag != null ? QuestTags.IndexOf(QuestPackExplorerModel.CurrentQuestTag) : -1;

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

            var ok = await QuestPackExplorerModel.AddQuest(tag);
            if (ok)
            {
                if (!QuestTags.Contains(tag)) QuestTags.Add(tag);
                OnPropertyChanged(nameof(QuestTags));
                OnPropertyChanged(nameof(SelectedPackFileName));
            }
        }

        private void RemoveQuest(string? tag)
        {
            Console.WriteLine("Removing quest with tag: " + tag);
            if (string.IsNullOrWhiteSpace(tag)) return;
            if (QuestPackExplorerModel.RemoveQuest(tag))
            {
                QuestTags.Remove(tag);
                OnPropertyChanged(nameof(QuestTags));
                OnPropertyChanged(nameof(SelectedPackFileName));
            }
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<object?, bool>? _canExecute = canExecute;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    internal sealed class AsyncRelayCommand<T>(Func<T?, Task> execute, Func<T?, bool>? canExecute = null) : ICommand
    {
        private readonly Func<T?, Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<T?, bool>? _canExecute = canExecute;

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);

        public async void Execute(object? parameter) => await _execute((T?)parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
