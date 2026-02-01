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
                if (SetProperty(ref _canvasPosition, value) && Connections.Count > 0)
                {
                    if (GetType().Equals(typeof(ConnectionInputVM)))
                    {
                        foreach(var conn in Connections)
                        {
                            conn.FromTo = new(conn.FromTo.From, value);
                        }
                    }
                    else
                    {
                        Connections.First().FromTo = new(value, Connections.First().FromTo.To);
                    }
                }
            }
        } Point _canvasPosition;

        public readonly HashSet<ConnectionVM> Connections = [];
    }
}
