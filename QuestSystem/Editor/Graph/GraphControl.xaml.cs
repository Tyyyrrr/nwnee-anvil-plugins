using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;


namespace QuestEditor.Graph
{
    public partial class GraphControl : UserControl
    {
        private Point draggedNodeGrabOffset;
        private NodeControl? draggedNode;
        private Canvas? _nodesCanvas;
        
        public GraphControl()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                _nodesCanvas = NodesItemsControl.FindChild<Canvas>();
                var ctx = (GraphVM)DataContext;
                NodesItemsControl.ItemContainerGenerator.StatusChanged += OnNodesGenerated;
            };
        }
        void OnNodesGenerated(object? sender, EventArgs e)
        {
            if (NodesItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                Dispatcher.BeginInvoke(
                    new Action(UpdateNodePositions),
                    DispatcherPriority.Loaded);
            }
        }


        public static readonly DependencyProperty DrawConnectionProperty = DependencyProperty.Register(
            "DrawConnection",
            typeof(bool),
            typeof(GraphControl),
            new PropertyMetadata(null));

        private ConnectionSocketControl? drawConnectionOrigin = null;
        public bool DrawConnection
        {
            get => (bool)GetValue(DrawConnectionProperty);
            set => SetValue(DrawConnectionProperty, value);
        }


        public static readonly DependencyProperty DrawConnectionFromToProperty = DependencyProperty.Register(
            "DrawConnectionFromTo",
            typeof(FromToPoint),
            typeof(GraphControl),
            new PropertyMetadata(null));

        public FromToPoint DrawConnectionFromTo
        {
            get => (FromToPoint)GetValue(DrawConnectionFromToProperty);
            set => SetValue(DrawConnectionFromToProperty, value);
        }


        public static readonly DependencyProperty EstablishConnectionCommandProperty = DependencyProperty.Register(
            "EstablishConnectionCommand",
            typeof(ICommand),
            typeof(GraphControl),
            new PropertyMetadata(null));

        public ICommand EstablishConnectionCommand
        {
            get => (ICommand)GetValue(EstablishConnectionCommandProperty);
            set => SetValue(EstablishConnectionCommandProperty, value);
        }

        public static readonly DependencyProperty ClearConnectionCommandProperty = DependencyProperty.Register(
            "ClearConnectionCommand",
            typeof(ICommand),
            typeof(GraphControl),
            new PropertyMetadata(null));

        public ICommand ClearConnectionCommand
        {
            get => (ICommand)GetValue(ClearConnectionCommandProperty);
            set => SetValue(ClearConnectionCommandProperty, value);
        }

        public void HandleSocketMouseUp(ConnectionSocketControl socket)
        {
            if (DrawConnection && drawConnectionOrigin != null && socket.CanBeTargeted)
            {
                EstablishConnectionCommand.Execute((drawConnectionOrigin.DataContext, socket.DataContext));
            }
            draggedNode = null;
            DrawConnection = false;
            drawConnectionOrigin = null;
            this.ReleaseMouseCapture();
        }

        public void HandleSocketMouseDown(ConnectionSocketControl socket)
        {
            if (!socket.CanBeTargeted) return; 
            ClearConnectionCommand.Execute(socket.DataContext);
            var pos = Mouse.GetPosition(this);
            var graphPos = ScreenToGraph(pos);
            DrawConnectionFromTo = new(graphPos, graphPos);
            drawConnectionOrigin = socket;
            DrawConnection = true;
        }
        public void HandleNodeMouseDown(NodeControl node)
        {
            draggedNode = node;
            draggedNodeGrabOffset = Mouse.GetPosition(node);
            this.CaptureMouse();
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            draggedNode = null;
            if(drawConnectionOrigin != null)
            {
                var node = ((GraphVM)DataContext).Nodes.FirstOrDefault(n=>n.ID == ((ConnectionSocketVM)drawConnectionOrigin.DataContext).SourceID);
                if(node != null)
                {
                    foreach (var o in node.OutputVMs)
                        if (o == drawConnectionOrigin.DataContext)
                        {
                            o.TargetID = -1;
                            break;
                        }
                }
            }
            DrawConnection = false;
            drawConnectionOrigin = null;
            this.ReleaseMouseCapture();
        }

        private Point _lastPanMousePos;

        void UpdateNodePositions()
        {
            var nodesControl = NodesItemsControl;
            var vm = (GraphVM)DataContext;
            foreach(var child in _nodesCanvas!.Children)
            {
                var cp = (ContentPresenter)child;
                var node = cp.FindChild<NodeControl>();
                if (node == null) continue;
                foreach (var s in node.GetSockets())
                {
                    var sPos = s.TranslatePoint(new(s.ActualWidth / 2, s.ActualHeight / 2), this);
                    s.CanvasPosition = ScreenToGraph(sPos);
                }
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (panMode)
            {
                var pos = e.GetPosition(this);
                var vm = (GraphVM)DataContext;
                var delta = pos - _lastPanMousePos;
                var pan = vm.Pan;
                pan.X += delta.X;
                pan.Y += delta.Y;
                _lastPanMousePos = pos;
            }

            if (draggedNode != null)
            {
                var screenPos = e.GetPosition(this);
                var graphPos = ScreenToGraph(screenPos);

                var nodePos = new Point(
                    graphPos.X - draggedNodeGrabOffset.X,
                    graphPos.Y - draggedNodeGrabOffset.Y
                );

                Canvas.SetLeft(draggedNode, nodePos.X);
                Canvas.SetTop(draggedNode, nodePos.Y);


                foreach (var s in draggedNode.GetSockets())
                {
                    var sPos = s.TranslatePoint(new(s.ActualWidth/2, s.ActualHeight/2), this);
                    s.CanvasPosition = ScreenToGraph(sPos);
                }
                
            }

            if (DrawConnection)
            {
                var screenPos = e.GetPosition(this); 
                var graphPos = ScreenToGraph(screenPos);
                DrawConnectionFromTo = new(DrawConnectionFromTo.From, graphPos);
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    panMode = true;
                    _lastPanMousePos = Mouse.GetPosition(this);
                    break;

                case Key.LeftShift:
                case Key.RightShift:
                    panMode = false;
                    break;


                default: break;
            }
        }

        private Point ScreenToGraph(Point p)
        {
            var vm = (GraphVM)DataContext;
            return new Point(
                (p.X - vm.Pan.X) / vm.Zoom.ScaleX,
                (p.Y - vm.Pan.Y) / vm.Zoom.ScaleY
            );
        }

        bool panMode = false;
        void ReleaseKeyModifiers()
        {
            panMode = false;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Keyboard.Focus(this);
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            ReleaseKeyModifiers();
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftShift:
                case Key.RightShift:
                    ReleaseKeyModifiers();
                    break;
            }
        }

        private const double zoomMin = 0.2;
        private const double zoomMax = 4;
        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vm = (GraphVM)DataContext;
            const double factor = 1.1;
            double scaleDelta = e.Delta > 0 ? factor : 1 / factor;

            var mouseScreen = e.GetPosition(this);

            var graphBefore = ScreenToGraph(mouseScreen);

            var zoom = vm.Zoom;

            double newScaleX = Math.Clamp(zoom.ScaleX * scaleDelta, zoomMin, zoomMax);
            double newScaleY = Math.Clamp(zoom.ScaleY * scaleDelta, zoomMin, zoomMax);

            zoom.ScaleX = newScaleX;
            zoom.ScaleY = newScaleY;

            vm.Pan.X = mouseScreen.X - graphBefore.X * zoom.ScaleX;
            vm.Pan.Y = mouseScreen.Y - graphBefore.Y * zoom.ScaleY;
        }

    }
}
