using QuestEditor.Shared;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public sealed class ConnectionSocketControl : Control
    {
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var graph = this.FindParent<GraphControl>();
            graph?.HandleSocketMouseDown(this);
            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            var graph = this.FindParent<GraphControl>();
            graph?.HandleSocketMouseUp(this);
            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        public static readonly DependencyProperty CanBeTargetedProperty = DependencyProperty.Register(
            "CanBeTargeted",
            typeof(bool),
            typeof(ConnectionSocketControl),
            new PropertyMetadata(false));

        public bool CanBeTargeted
        {
            get => (bool)GetValue(CanBeTargetedProperty);
            set => SetValue(CanBeTargetedProperty, value);
        }

        public static readonly DependencyProperty CanvasPositionProperty = DependencyProperty.Register(
            "CanvasPosition",
            typeof(Point),
            typeof(ConnectionSocketControl),
            new PropertyMetadata(default));

        public Point CanvasPosition
        {
            get => (Point)GetValue(CanvasPositionProperty);
            set => SetValue(CanvasPositionProperty, value);
        }

        public static readonly DependencyProperty SocketColorBrushProperty = DependencyProperty.Register(
            "SocketColorBrush",
            typeof(SolidColorBrush),
            typeof(ConnectionSocketControl),
            new PropertyMetadata(default));

        public SolidColorBrush SocketColorBrush
        {
            get => (SolidColorBrush)GetValue(SocketColorBrushProperty);
            set => SetValue(SocketColorBrushProperty, value);
        }

        static ConnectionSocketControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionSocketControl), new FrameworkPropertyMetadata(typeof(ConnectionSocketControl)));
        }

    }
}
