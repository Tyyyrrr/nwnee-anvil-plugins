using QuestEditor.Explorer;
using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public sealed class GraphVM : ViewModelBase
    {
        public ObservableCollection<NodeVM> Nodes { get; private set; } = [];
        public ObservableCollection<ConnectionVM> Connections { get; private set; } = [];

        public ICommand EstablishConnectionCommand { get; }
        public ICommand ClearConnectionCommand { get; }

        public ScaleTransform Zoom { get; } = new ScaleTransform(1.0, 1.0);
        public TranslateTransform Pan { get; } = new TranslateTransform(0, 0);

        public GraphVM()
        {
            EstablishConnectionCommand = new RelayCommand(EstablishConnection, _ => true);
            ClearConnectionCommand = new RelayCommand(ClearConnection, _ => true);
        }

        void EstablishConnection(object? parameter)
        {
            if (parameter is not ValueTuple<object, object> sockets || sockets.Item1 is not ConnectionOutputVM output || sockets.Item2 is not ConnectionInputVM input)
                return;

            var outPos = output.CanvasPosition;
            var inPos = input.CanvasPosition;
            Trace.WriteLine($"Establishing connection between [ {output.SourceID} : {outPos} ] and [ {input.SourceID} : {inPos} ]");
            if(output.Connection?.Input == input)
            {
                Trace.WriteLine($"Already connected");
                return;
            }
            else if(output.Connection != null)
            {
                ClearConnection(output.Connection);
                Trace.WriteLine("Clearing old connection");
            }
            var conn = new ConnectionVM(output, input);
            output.Connection = conn;
            input.Connection = conn;
            // todo: update NextIDs
            Connections.Add(conn);
        }
        void ClearConnection(object? parameter)
        {
            ConnectionVM? connectionVM = null;

            if(parameter is ConnectionSocketVM socketVM)
                connectionVM = socketVM.Connection;
            else if(parameter is ConnectionVM)
                connectionVM = (ConnectionVM)parameter;

            if (connectionVM != null)
            {
                if (connectionVM.Output != null)
                    connectionVM.Output.Connection = null;
                if(connectionVM.Input != null)
                    connectionVM.Input.Connection = null;

                // todo: update NextIDs
                Connections.Remove(connectionVM);
            }

        }

        public ConnectionVM DrawConnectionContext { get; } = new();
        public bool DrawConnection
        {
            get => _drawConnection;
            set 
            {
                if (SetProperty(ref _drawConnection, value))
                {
                    if (value && !Connections.Contains(DrawConnectionContext))
                    {
                        foreach(var node in Nodes)
                        {
                            node.IsInputAvailable = true;
                            node.IsOutputAvailable = false;
                        }
                        Connections.Add(DrawConnectionContext);
                    }
                    else if (!value)
                    {
                        foreach (var node in Nodes)
                        {
                            node.IsInputAvailable = false;
                            node.IsOutputAvailable = true;
                        }
                        Connections.Remove(DrawConnectionContext);
                    }
                }
            }
        }bool _drawConnection;

        public QuestVM? CurrentQuest
        {
            get => _currentQuest;
            set
            {
                if (SetProperty(ref _currentQuest, value))
                {
                    if(value == null)
                    {
                        Nodes.CollectionChanged -= OnNodesCollectionChanged;
                        Nodes = [];
                        Connections.Clear();
                    }
                    else
                    {
                        Nodes = value.Nodes;

                        Nodes.CollectionChanged += OnNodesCollectionChanged;
                        ReconnectNodes();
                    }

                    RaisePropertyChanged(nameof(Nodes));
                }
            }
        }QuestVM? _currentQuest = null;

        void OnNodesCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            //Trace.WriteLine("Nodes collection changed action: " + e.Action.ToString());
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null) return;
                    foreach(var item in e.NewItems)
                    {
                        if (item is not NodeVM node) continue;
                    }
                    ReconnectNodes();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if(e.OldItems == null) return;
                    foreach(var item in e.OldItems)
                    {
                        if (item is not NodeVM node) continue;
                    }
                    ReconnectNodes();
                    break;
            }
        }

        private void ReconnectNodes()
        {
            //Connections.Clear();

            //foreach(var node in Nodes)
            //{
            //    var outputs = node.OutputVMs;

            //    foreach(var output in outputs)
            //    {
            //        if (output.TargetID < 0 || output.TargetID == node.ID) continue;

            //        var input = Nodes.FirstOrDefault(n => n.ID == output.TargetID)?.InputVM ?? null;

            //        if (input != null) Connections.Add(new(output, input));
            //    }
            //}
        }
    }
}
