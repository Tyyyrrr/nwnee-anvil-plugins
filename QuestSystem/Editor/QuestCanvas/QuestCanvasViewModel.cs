using System.Collections.ObjectModel;
using System.Windows.Media;
using QuestEditor.Shared;
using QuestEditor.StageNode;

namespace QuestEditor.QuestCanvas
{
    public sealed class QuestCanvasViewModel : ViewModelBase
    {        
        private static readonly SolidColorBrush _canvasBorderBrush = new(Color.FromRgb(15,15,15) * 5);
        private static readonly SolidColorBrush _canvasBackgroundBrush = new(Color.FromRgb(25,20,45) * 4);
        public Brush CanvasBorderBrush => _canvasBorderBrush;
        public Brush CanvasBackgroundBrush => _canvasBackgroundBrush;

        public QuestCanvasViewModel()
        {
            
        }

        ObservableCollection<StageNodeView> StageNodes {get;set;} = [];
    }
}