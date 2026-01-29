using QuestEditor.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace QuestEditor.Graph
{
    public partial class GraphControl : UserControl
    {
        private NodeVM? _draggedNode = null;
        private Canvas? _nodesCanvas;

        public GraphControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _nodesCanvas = FindChild<Canvas>(NodesItemsControl)
                           ?? throw new InvalidOperationException("Canvas not found");
        }
        public static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed)
                    return typed;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        public void BeginDrag(NodeVM node)
        {
            _draggedNode = node;
            _nodesCanvas!.CaptureMouse();
        }
        public static T? FindItemsPanel<T>(ItemsControl itemsControl) where T : Panel
        {
            return itemsControl.ItemsPanel.LoadContent() as T;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _draggedNode = null;
            _nodesCanvas!.ReleaseMouseCapture();
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedNode == null)
                return;

            var pos = e.GetPosition(_nodesCanvas);

            _draggedNode.CanvasLeft = pos.X;
            _draggedNode.CanvasTop = pos.Y;
        }


    }
}
