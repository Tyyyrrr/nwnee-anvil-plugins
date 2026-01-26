using QuestEditor.Shared;
using QuestSystem.Nodes;

namespace QuestEditor.Nodes
{
    public abstract class NodeVM(NodeBase node) : ViewModelBase, ISelectable
    {
        public static NodeVM? SelectViewModel(NodeBase node)
        {
            if (node is StageNode stageNode) return new StageNodeVM(stageNode);
            else if (node is RewardNode rewardNode) return new RewardNodeVM(rewardNode);
            else return null;
        }

        protected virtual NodeBase Node => node;

        public int ID => node.ID;
        public int NextID
        {
            get => _nextID;
            set { if (SetProperty(ref _nextID, value)) node.NextID = _nextID; }
        } private int _nextID;

        public string NodeType { get; } = node.GetType().Name;

        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = false;
        public void Select()
        {
            throw new NotImplementedException();
        }

        public void ClearSelection()
        {
            throw new NotImplementedException();
        }
    }
}