using QuestEditor.Explorer;
using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

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

            Trace.WriteLine($"Establishing connection between {output.SourceID} and {input.SourceID}");

            // Refuse to connect node to itself
            if(input.SourceID == output.SourceID)
            {
                Trace.WriteLine("Can't connect to self"); // temporary (?)
                return;
            }

            // Early return if this connection already exists
            if(input.Connections.Any(c=>c.Output == output))
            {
                Trace.WriteLine($"Already connected");
                return;
            }

            // Remove existing connection from output socket if applicable
            var existingOutputConnection = output.Connections.FirstOrDefault();
            if (existingOutputConnection != null)
            {
                Trace.WriteLine("Removing existing connection");
                existingOutputConnection.Input?.Connections.Remove(existingOutputConnection);
                output.Connections.Clear();
                Connections.Remove(existingOutputConnection);
            }

            // find source node and its output index
            NodeVM? inputNode = null;
            NodeVM? outputNode = null;
            int outputIndex = -1;
            foreach (var node in Nodes)
            {
                if(node.ID == output.SourceID)
                {
                    outputNode = node;
                    for(int i = 0; i < outputNode.OutputVMs.Count; i++)
                        if (outputNode.OutputVMs[i] == output)
                        {
                            outputIndex = i;
                            break;
                        }
                    if (inputNode != null)
                        break;
                }
            }


            if (outputNode == null) throw new InvalidOperationException("Output node not found");
            if (outputIndex < 0) throw new InvalidOperationException("Output index not found");


            var outPos = output.CanvasPosition;
            var inPos = input.CanvasPosition;

            var conn = new ConnectionVM(output, input);

            output.Connections.Add(conn);
            input.Connections.Add(conn);

            Trace.WriteLine($"Connection between {outputNode.ID}:{outputIndex} -> {input.SourceID} established");
            outputNode.SetNextID(input.SourceID, outputIndex);
        }
        void ClearConnection(object? parameter)
        {
            ConnectionVM? connectionVM = null;

            if(parameter is ConnectionOutputVM socketVM)
                connectionVM = socketVM.Connections.FirstOrDefault();
            else if(parameter is ConnectionVM)
                connectionVM = (ConnectionVM)parameter;

            if (connectionVM != null && connectionVM.Output != null)
            {
                var node = Nodes.FirstOrDefault(n => n.ID == connectionVM.Output.SourceID);
                if(node != null)
                {
                    var outputs = node.OutputVMs;
                    int outputIndex = 0;
                    foreach (var output in node.OutputVMs)
                        if (output != connectionVM.Output)
                        {
                            outputIndex++;
                        }
                        else break;

                    node?.SetNextID(-1,outputIndex);
                }
                connectionVM.Output?.Connections.Clear();
                connectionVM.Input?.Connections.Remove(connectionVM);
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

        public bool IsGraphActive => _currentQuest != null;
        public QuestVM? CurrentQuest
        {
            get => _currentQuest;
            set
            {
                if(value != _currentQuest && _currentQuest != null)
                {
                    _currentQuest.SaveNodePositions();
                }

                if (SetProperty(ref _currentQuest, value))
                {
                    if(value == null)
                    {
                        Nodes.CollectionChanged -= OnNodesCollectionChanged;

                        foreach (var node in Nodes)
                        {
                            node.OutputChanged -= OnNodeOutputChanged;
                        }

                            Nodes = [];
                        foreach(var conn in Connections)
                        {
                            if (conn.Input != null) conn.Input.Connections.Clear();
                            if (conn.Output != null) conn.Output.Connections.Clear();
                        }
                        Connections.Clear();
                    }
                    else
                    {
                        Nodes = value.Nodes;
                        Nodes.CollectionChanged += OnNodesCollectionChanged;
                        foreach (var node in Nodes)
                        {
                            if (value.NodePositions.TryGetValue(node.ID, out var pos))
                                node.CanvasPosition = pos;

                            //Trace.WriteLine("Restored node position at " + pos);
                            node.OutputChanged += OnNodeOutputChanged;
                        }

                        ReconnectNodes();

                    }

                    Pan.X = 0;
                    Pan.Y = 0;
                    Zoom.ScaleX = 1;
                    Zoom.ScaleY = 1;
                    RaisePropertyChanged(nameof(Nodes));
                    RaisePropertyChanged(nameof(IsGraphActive));
                }
            }
        }QuestVM? _currentQuest = null;

        void OnNodesCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null) return;
                    foreach(var item in e.NewItems)
                    {
                        if (item is not NodeVM node) continue;
                        node.OutputChanged += OnNodeOutputChanged;
                    }
                    ReconnectNodes();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if(e.OldItems == null) return;
                    foreach(var item in e.OldItems)
                    {
                        if (item is not NodeVM node) continue;
                        node.OutputChanged -= OnNodeOutputChanged;
                    }
                    ReconnectNodes();
                    break;
            }
        }

        void OnNodeOutputChanged(object s, (int,int) fromTo)
        {
            var node = (NodeVM)s;
            var outputIndex = fromTo.Item1;
            var targetIndex = fromTo.Item2;
            //Trace.WriteLine($"On node connection changed {node.ID}:{outputIndex} points to {targetIndex}");

            var output = node.OutputVMs[outputIndex];
            if(output.Connections.Count > 0)
            {
                //Trace.WriteLine("Clearing old connection from output no. " + outputIndex);
                var c = output.Connections.First();

                if(c != null)
                {

                    c.Input?.Connections.Remove(c);
                    Connections.Remove(c);
                    output.Connections.Clear();
                }

            }

            if (targetIndex < 0) return;

            var targetNode = Nodes.FirstOrDefault(n => n.ID == targetIndex);
            if (targetNode == null)
            {
                Trace.WriteLine("Target node not found");
                return;
            }

            var conn = new ConnectionVM(output, targetNode.InputVM);
            targetNode.InputVM.Connections.Add(conn);
            output.Connections.Add(conn);
            Connections.Add(conn);
        }

        private void ReconnectNodes()
        {
            //Trace.WriteLine("Reconnecting all nodes");
            Connections.Clear();

            foreach (var node in Nodes)
            {
                var outputs = node.OutputVMs;

                for(int i = 0; i < outputs.Count; i++)
                {
                    OnNodeOutputChanged(node, (i, outputs[i].TargetID));
                }

            }
        }
    }
}
