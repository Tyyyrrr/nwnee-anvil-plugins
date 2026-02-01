using QuestEditor.Shared;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public sealed class ConnectionVM : ViewModelBase
    {
        public FromToPoint FromTo
        {
            get => _fromTo;
            set => SetProperty(ref _fromTo, value);
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
        LinearGradientBrush _currentGradientBrush = new (Colors.Red, Colors.Green, 0);//, _normalGradientBrush, _inverseGradientBrush;

        public ConnectionVM() { }

        public ConnectionVM(ConnectionOutputVM output, ConnectionInputVM input)
        {
            _output = output;
            _input = input;
            FromTo = new FromToPoint(output.CanvasPosition, input.CanvasPosition);
        }
    }
}
