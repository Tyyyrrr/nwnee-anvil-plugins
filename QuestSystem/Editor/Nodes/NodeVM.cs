using QuestEditor.Explorer;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Diagnostics;
using System.Windows.Input;

namespace QuestEditor.Nodes
{
    public abstract class NodeVM : StatefulViewModelBase, ISelectable
    {
        private NodeBase model;
        private NodeBase clone;
        public NodeBase Model => clone;
        public NodeVM(NodeBase node, QuestVM quest) : base(quest)
        {
            model = node;
            clone = (NodeBase)node.Clone();
            NodeType = clone.GetType().Name;
            DeleteNodeCommand = new RelayCommand(quest.RemoveNode, _=>true);
        }
        public static NodeVM? SelectViewModel(NodeBase node, QuestVM quest)
        {
            if (node is StageNode stageNode) return new StageNodeVM(stageNode, quest);
            else if (node is RewardNode rewardNode) return new RewardNodeVM(rewardNode, quest);
            else return null;
        }

        public ICommand DeleteNodeCommand { get; }

        protected virtual NodeBase Node => clone;

        public int ID => clone.ID;
        public int NextID
        {
            get => _nextID;
            set { if (SetProperty(ref _nextID, value)) clone.NextID = _nextID; }
        } private int _nextID;

        public string NodeType { get; }

        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = false;
        public void Select()
        {
            Trace.WriteLine("Node select");
        }

        public void ClearSelection()
        {
            Trace.WriteLine("Node clear selection");
        }

        protected override void Apply()
        {
            Trace.WriteLine("Node apply");
            model = clone;
            clone = (NodeBase)clone.Clone();
        }

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => null;
        public override void RefreshIsDirty()
        {
            base.RefreshIsDirty();
        }
    }
}