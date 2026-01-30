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

        public event Action<NodeVM, int>? OutputChanged;

        public int ID => Model.ID;

        public int NextID
        {
            get => Model.NextID;
            set
            {
                if(Model.NextID == value) return;

                Model.NextID = value;
                OutputChanged?.Invoke(this, value);
                RaisePropertyChanged(nameof(NextID));
                RaisePropertyChanged(nameof(NextIDString));
            }

        }

        public string NextIDString
        {
            get => NextID.ToString();
            set
            {
                if (!int.TryParse(value, out var nextID) || NextID == nextID)
                    return;

                PushOperation(new SetNextIDOperation(this, nextID));
            }
        }

        public bool Rollback
        {
            get => Model.Rollback;
            set
            {
                if(Model.Rollback == value) return;

                PushOperation(new SetRollbackOperation(this, value));
            }
        }


        public string NodeType { get; }

        private sealed class SetNextIDOperation(NodeVM node, int newVal) : UndoableOperation(node)
        {
            private readonly int _oldVal = node.NextID;
            private readonly int _newVal = newVal;
            protected override void ProtectedDo()
            {
                ((NodeVM)Origin).NextID = _newVal;
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                ((NodeVM)Origin).NextID = _oldVal;
            }
        }

        private sealed class SetRollbackOperation(NodeVM node, bool rollback) : UndoableOperation(node)
        {
            private bool _oldVal = node.Rollback;
            private bool _rollback = rollback;

            protected override void ProtectedDo()
            {
                var vm = (NodeVM)Origin;
                vm.Model.Rollback = _rollback;
                vm.RaisePropertyChanged(nameof(Rollback));
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                var vm = (NodeVM)Origin;
                vm.Model.Rollback = _oldVal;
                vm.RaisePropertyChanged(nameof(Rollback));
            }
        }
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

        public double CanvasLeft
        {
            get => _canvasLeft;
            set => SetProperty(ref _canvasLeft, value);
        }
        double _canvasLeft;

        public double CanvasTop
        {
            get => _canvasTop;
            set => SetProperty(ref _canvasTop, value);
        }
        double _canvasTop;

    }
}