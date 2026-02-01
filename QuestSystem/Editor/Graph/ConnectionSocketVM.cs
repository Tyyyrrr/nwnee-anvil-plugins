using QuestEditor.Shared;
using System.Windows;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public abstract class ConnectionSocketVM(int sourceID) : ViewModelBase
    {
        public int SourceID { get; } = sourceID;
        public Color SocketColor => SocketColorBrush.Color;
        public SolidColorBrush SocketColorBrush
        {
            get => _socketColorBrush;
            set
            {
                if (SetProperty(ref _socketColorBrush, value))
                    RaisePropertyChanged(nameof(SocketColor));
            }
        }
        SolidColorBrush _socketColorBrush = Brushes.Black;

        public bool CanBeTargeted
        {
            get => _canBeTargeted;
            set => SetProperty(ref _canBeTargeted, value);
        }bool _canBeTargeted;

        public Point CanvasPosition
        {
            get => _canvasPosition;
            set
            {
                if (SetProperty(ref _canvasPosition, value) && Connection != null)
                {
                    if (GetType().Equals(typeof(ConnectionInputVM)))
                    {
                        Connection.FromTo = new(Connection.FromTo.From, value);
                    }
                    else
                    {
                        Connection.FromTo = new(value, Connection.FromTo.To);
                    }
                }
            }
        } Point _canvasPosition;

        public ConnectionVM? Connection
        {
            get => _connection;
            set => SetProperty(ref _connection, value);
        } ConnectionVM? _connection;
    }
}
