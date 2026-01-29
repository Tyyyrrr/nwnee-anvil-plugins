using QuestEditor.Explorer;
using QuestEditor.Nodes;
using QuestEditor.Shared;
using System.Collections.ObjectModel;

namespace QuestEditor.Graph
{
    public sealed class GraphVM : ViewModelBase
    {
        public ObservableCollection<NodeVM> Nodes { get; private set; } = [];
        public ObservableCollection<ConnectionVM> Connections { get; private set; } = [];

        public QuestVM? CurrentQuest
        {
            get => _currentQuest;
            set
            {
                if (SetProperty(ref _currentQuest, value))
                {
                    if(value == null)
                    {
                        foreach (var node in Nodes)
                            node.OutputChanged -= OnNodeOutputChanged;
                        Nodes = [];
                        Connections.Clear();
                    }
                    else
                    {
                        Nodes = value.Nodes;
                        foreach (var node in Nodes)
                            node.OutputChanged += OnNodeOutputChanged;
                        ReconnectNodes();
                    }

                    RaisePropertyChanged(nameof(Nodes));
                }
            }
        }QuestVM? _currentQuest = null;

        private void ReconnectNodes()
        {
            Connections.Clear();
            foreach(var node in Nodes)
            {
                NodeVM from;
                NodeVM? to;
                if (node.ID != node.NextID && node.NextID >= 0)
                {
                    from = node;
                    to = Nodes.FirstOrDefault(n => n.ID == from.NextID);
                    if (to == null) continue;

                    var connection = new ConnectionVM(from, to);
                    Connections.Add(connection);
                }
                else continue;
            }
        }
        void OnNodeOutputChanged(NodeVM node, int nextID)
        {
            var connection = Connections.FirstOrDefault(c => c.FromNode == node);
            if (connection != null) Connections.Remove(connection);

            if (node.ID == nextID || node.NextID < 0) return;

            var toNode = Nodes.FirstOrDefault(n => n.ID == nextID);

            if (toNode == null) return;

            connection = new ConnectionVM(node, toNode);
            Connections.Add(connection);
        }
    }
}
