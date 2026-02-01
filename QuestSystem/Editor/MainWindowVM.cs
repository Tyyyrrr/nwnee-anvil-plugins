using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Inspector;
using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.Diagnostics;
using System.Windows.Input;

namespace QuestEditor
{
    internal class MainWindowVM : ViewModelBase
    {
        private const int MaxUndos = 20;
        public ExplorerVM Explorer { get; } = new();
        public GraphVM Graph { get; } = new();
        public InspectorVM Inspector { get; } = new();

        private readonly List<UndoableOperation> _undo = new(MaxUndos);
        private readonly List<UndoableOperation> _redo = new(MaxUndos);

        public MainWindowVM()
        {
            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);

            Explorer.Recorded += OnUndoableOperationPushed;
            Explorer.Discarded += OnChangesDiscarded;
            Explorer.QuestSelected += q => Graph.CurrentQuest = q;
        }

        void OnUndoableOperationPushed(UndoableOperation op)
        {
            if(_undo.Count == MaxUndos)
                _undo.RemoveAt(0);
            
            _undo.Add(op);
            _redo.Clear();
            op.Do();

            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }

        void DropOperations(IReadOnlyList<IStateful> discardedOrigins)
        {
            Trace.WriteLine("Dropping operations from " + discardedOrigins.Count.ToString() + " origins");
            _undo.RemoveAll(op => discardedOrigins.Contains(op.Origin));
            _redo.RemoveAll(op => discardedOrigins.Contains(op.Origin));

            ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        }
        void OnChangesDiscarded(IStateful origin, IReadOnlyList<IStateful> discarded)
        {
            DropOperations(discarded);

            switch (origin)
            {
                case ExplorerVM explorer:
                    explorer.ReopenAllPacks();
                    explorer.RefreshIsDirty();
                    break;
                case QuestPackVM questPack:
                    questPack.ResetTemporaryFile();
                    questPack.ReloadAllQuests();
                    questPack.RefreshIsDirty();
                    break;
                case QuestVM quest:
                    quest.PackVM.ReloadSingleQuest(quest);
                    quest.RefreshIsDirty();
                    break;
                case NodeVM node:
                    Trace.WriteLine("Discarding node changes is not implemented");
                    node.RefreshIsDirty();
                    break;
            }

            Trace.WriteLine($"Dropped {discarded.Count} operations. New count: {_undo.Count} (undo), {_redo.Count} (redo)");
        }


        public ICommand HelpCommand { get; } = new RelayCommand(Help, _ => true);
        static void Help(object? _) => Trace.WriteLine("There is no help.");
        
        public ICommand CloseCommand { get; } = new RelayCommand(Close, _ => true);
        static void Close(object? _) => App.Current.MainWindow.Close();

        public ICommand UndoCommand { get; }
        void Undo(object? _)
        {
            if (!CanUndo(_)) return;
            var op = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            op.Undo();
            _redo.Add(op);
        }
        bool CanUndo(object? _) => _undo.Count != 0;

        public ICommand RedoCommand { get; }
        void Redo(object? _)
        {
            if (!CanRedo(_)) return;
            var op = _redo[^1];
            _redo.RemoveAt(_redo.Count - 1);
            op.Redo();
            _undo.Add(op);
        }
        bool CanRedo(object? _) => _redo.Count != 0;
    }
}
