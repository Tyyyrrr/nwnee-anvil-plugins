using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public sealed class ConnectionVM : ViewModelBase
    {
        /// Temporary solution
        static LinearGradientBrush _stageToStageGradient, _stageToStageGradient_Inverse;
        static LinearGradientBrush _fromStageGradient, _fromStageGradient_Inverse;
        static LinearGradientBrush _toStageGradient, _toStageGradient_Inverse;
        static LinearGradientBrush _regularGradient, _regularGradient_Inverse;
        static ConnectionVM()
        {
            _stageToStageGradient = new LinearGradientBrush(Colors.ForestGreen, Colors.IndianRed, 0d);
            _stageToStageGradient_Inverse = new LinearGradientBrush(Colors.IndianRed, Colors.ForestGreen, 0d);

            _fromStageGradient = new LinearGradientBrush(Colors.ForestGreen, Colors.Indigo, 0d);
            _fromStageGradient_Inverse = new LinearGradientBrush(Colors.Indigo, Colors.ForestGreen, 0d);

            _toStageGradient = new LinearGradientBrush(Colors.LightSteelBlue, Colors.IndianRed, 0d);
            _toStageGradient_Inverse = new LinearGradientBrush(Colors.IndianRed, Colors.LightSteelBlue, 0d);

            _regularGradient = new LinearGradientBrush(Colors.LightSteelBlue, Colors.Indigo, 0d);
            _regularGradient_Inverse = new LinearGradientBrush(Colors.Indigo, Colors.LightSteelBlue, 0d);
        }
        /// 




        private readonly NodeVM _from, _to;
        public NodeVM FromNode => _from;
        public NodeVM ToNode => _to;

        public FromToPoint FromToPoint
        {
            get => _fromToPoint;
            private set => SetProperty(ref _fromToPoint, value);
        }private FromToPoint _fromToPoint;

        public LinearGradientBrush FromToColorGradientBrush
        {
            get => _currentGradientBrush;
            private set => SetProperty(ref _currentGradientBrush, value);
        }
        LinearGradientBrush _currentGradientBrush, _normalGradientBrush, _inverseGradientBrush;


        public ConnectionVM(NodeVM from, NodeVM to)
        {
            _from = from;
            _to = to;

            from.PropertyChanged += OnNodePropertyChanged;
            to.PropertyChanged += OnNodePropertyChanged;

            if(to is StageNodeVM && from is StageNodeVM)
            {
                _normalGradientBrush = _stageToStageGradient;
                _inverseGradientBrush = _stageToStageGradient_Inverse;
            }
            else if(from is StageNodeVM)
            {
                _normalGradientBrush = _fromStageGradient;
                _inverseGradientBrush = _fromStageGradient_Inverse;
            }
            else if(to is StageNodeVM)
            {
                _normalGradientBrush = _toStageGradient;
                _inverseGradientBrush = _toStageGradient_Inverse;
            }
            else
            {
                _normalGradientBrush = _regularGradient;
                _inverseGradientBrush = _regularGradient_Inverse;
            }

            _currentGradientBrush = from.CanvasLeft >= to.CanvasLeft ? _inverseGradientBrush : _normalGradientBrush;

            _fromToPoint = new(
                new Point(_from.CanvasLeft, _from.CanvasTop),
                new Point(_to.CanvasLeft, _to.CanvasTop)
                );
        }

        void OnNodePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if((e.PropertyName == nameof(NodeVM.CanvasLeft) || e.PropertyName == nameof(NodeVM.CanvasTop)))
            {
                if(_from.CanvasLeft >=  _to.CanvasLeft)
                {
                    if (FromToColorGradientBrush != _inverseGradientBrush)
                        FromToColorGradientBrush = _inverseGradientBrush;
                }
                else if (FromToColorGradientBrush != _normalGradientBrush)
                    FromToColorGradientBrush = _normalGradientBrush;

                FromToPoint = new(
                    new Point(_from.CanvasLeft, _from.CanvasTop),
                    new Point(_to.CanvasLeft, _to.CanvasTop)
                    );
            }
        }
    }
}
