using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using QuestEditor.ObjectiveBox;
using QuestEditor.QuestPackExplorer;
using QuestEditor.RewardBox;
using QuestEditor.StageNode;

namespace QuestEditor.QuestCanvas
{
    public sealed class QuestCanvasViewModel : INotifyPropertyChanged
    {        
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private readonly QuestPackExplorerViewModel _qpevm;
        private readonly StageNodeViewModel _snvm;
        private readonly ObjectiveBoxViewModel _obvm;
        private readonly RewardBoxViewModel _rbvm;

        public QuestPackExplorerViewModel ExplorerVM => _qpevm;

        private static readonly SolidColorBrush _canvasBorderBrush = new(Color.FromRgb(15,15,15) * 5);
        private static readonly SolidColorBrush _canvasBackgroundBrush = new(Color.FromRgb(25,20,45) * 4);
        public Brush CanvasBorderBrush => _canvasBorderBrush;
        public Brush CanvasBackgroundBrush => _canvasBackgroundBrush;
        public QuestCanvasViewModel(QuestPackExplorerViewModel qpevm)
        {
            _qpevm = qpevm;
            _qpevm.OnRefreshFromModel += OnExplorerRefresh;
        }

        ObservableCollection<StageNodeView> StageNodes {get;set;} = [];

        void OnExplorerRefresh()
        {
            var stages = QuestPackExplorerModel.GetStagesOfCurrentQuest();
            StageNodes.Clear();
            foreach(var stage in stages)
            {
                var node = new StageNodeView();
                node.DataContext = new StageNodeViewModel(new StageNodeModel(stage));
            }
        }
    }
}