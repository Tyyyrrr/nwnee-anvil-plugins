using QuestEditor.Shared;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public sealed class ConnectionVM : ViewModelBase
    {
        public FromToPoint FromTo
        {
            get => _fromTo;
            set
            {
                if (SetProperty(ref _fromTo, value))
                {
                    if (_fromTo.From.X < _fromTo.To.X)
                    {
                        if (FromToColorGradientBrush != _normalGradientBrush)
                            FromToColorGradientBrush = _normalGradientBrush;
                    }
                    else
                    {
                        if (FromToColorGradientBrush != _inverseGradientBrush)
                            FromToColorGradientBrush = _inverseGradientBrush;
                    }
                }
            }
        }
        FromToPoint _fromTo;

        public ConnectionOutputVM? Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        } ConnectionOutputVM? _output;

        public ConnectionInputVM? Input
        {
            get => _input;
            set => SetProperty(ref _input, value);
        } ConnectionInputVM? _input;

        public LinearGradientBrush FromToColorGradientBrush
        {
            get => _currentGradientBrush;
            private set => SetProperty(ref _currentGradientBrush, value);
        }
        LinearGradientBrush _currentGradientBrush, _normalGradientBrush, _inverseGradientBrush;

        public ConnectionVM()
        {
            _normalGradientBrush = new(Colors.Red, Colors.Green, 0);
            _inverseGradientBrush = new(Colors.Green, Colors.Red, 0);
            _currentGradientBrush = _normalGradientBrush;
        }

        public ConnectionVM(ConnectionOutputVM output, ConnectionInputVM input)
        {
            _output = output;
            _input = input;
            FromTo = new FromToPoint(output.CanvasPosition, input.CanvasPosition);
            var outCol = output.SocketColor;
            var inCol = input.SocketColor;
            _normalGradientBrush = new(outCol, inCol,0);
            _inverseGradientBrush = new(inCol, outCol, 0);
            _currentGradientBrush = output.CanvasPosition.X < input.CanvasPosition.X ? _normalGradientBrush : _inverseGradientBrush;
        }
    }
}
