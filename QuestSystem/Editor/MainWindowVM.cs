using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Inspector;
using QuestEditor.Shared;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace QuestEditor
{
    internal class MainWindowVM : ViewModelBase
    {
        public ExplorerVM Explorer { get; } = new();
        public GraphVM Graph { get; } = new();
        public InspectorVM Inspector { get; } = new();

        private Stack<StatefulViewModelBase> _undoStack = [];
        private Stack<StatefulViewModelBase> _redoStack = [];

        public MainWindowVM()
        {
            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);

            Explorer.Recorded += OnUndoableOperationPushed;
            Graph.Recorded += OnUndoableOperationPushed;
            Inspector.Recorded += OnUndoableOperationPushed;
        }

        void OnUndoableOperationPushed(StatefulViewModelBase sender)
        {
            _undoStack.Push(sender);
            _redoStack.Clear();
            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }

        public ICommand HelpCommand { get; } = new RelayCommand(Help, _ => true);
        static void Help(object? _) => Trace.WriteLine("Help command executed");
        
        public ICommand CloseCommand { get; } = new RelayCommand(Close, _ => true);
        static void Close(object? _) => Application.Current.Shutdown(0);


        public ICommand UndoCommand { get; }
        void Undo(object? _)
        {
            if (!CanUndo(_)) return;
            var stateful = _undoStack.Pop();
            stateful.Undo();
            _redoStack.Push(stateful);
        }
        bool CanUndo(object? _) => _undoStack.TryPeek(out var stateful) && stateful.CanUndo;

        public ICommand RedoCommand { get; }
        void Redo(object? _)
        {
            if (!CanRedo(_)) return;
            var stateful = _redoStack.Pop();
            stateful.Redo();
            _undoStack.Push(stateful);
        }
        bool CanRedo(object? _) => _redoStack.TryPeek(out var stateful) && stateful.CanRedo;
    }
}
