using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace QuestEditor.Nodes
{
    public abstract class NodeVM : StatefulViewModelBase, ISelectable
    {
        private NodeBase model;
        private NodeBase clone;
        public NodeBase Model => clone;

        public ConnectionInputVM InputVM
        {
            get => _inputVM;
            set => SetProperty(ref _inputVM, value);
        }private ConnectionInputVM _inputVM;

        public bool IsOutputAvailable
        {
            get => _isOutputAvailable;
            set
            {
                if(SetProperty(ref _isOutputAvailable, value))
                {
                    foreach (var oVM in OutputVMs)
                        oVM.CanBeTargeted = value;
                }
            }
        } bool _isOutputAvailable;

        public bool IsInputAvailable
        {
            get => _isInputAvailable;
            set
            {
                if (SetProperty(ref _isInputAvailable, value))
                    _inputVM.CanBeTargeted = value;
            }
        } private bool _isInputAvailable;

        public abstract IReadOnlyList<ConnectionOutputVM> OutputVMs { get; }




        public NodeVM(NodeBase node, QuestVM quest) : base(quest)
        {
            model = node;
            _inputVM = new(node.ID);
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

        public int ID => Model.ID;
        public int TargetID => Model.NextID;

        public int NextID
        {
            get => Model.NextID;
            set
            {
                if(Model.NextID == value) return;

                Model.NextID = value;
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

        public Point CanvasPosition
        {
            get => _canvasPosition;
            set
            {
                if (SetProperty(ref _canvasPosition, value))
                {
                    _canvasLeft = value.X;
                    _canvasTop = value.Y;
                    RaisePropertyChanged(nameof(CanvasLeft));
                    RaisePropertyChanged(nameof(CanvasTop));
                }
            }
        }
        Point _canvasPosition;
        public double CanvasLeft
        {
            get => _canvasLeft;
            set
            {
                if (SetProperty(ref _canvasLeft, value))
                    CanvasPosition = new(value,_canvasTop);
            }
        }
        double _canvasLeft;

        public double CanvasTop
        {
            get => _canvasTop;
            set
            {
                if (SetProperty(ref _canvasTop, value))
                    CanvasPosition = new(_canvasLeft, value);
            }
        }
        double _canvasTop;
    }

    public abstract class SingleOutputNodeVM : NodeVM
    {
        public ConnectionOutputVM OutputVM
        {
            get => _outputVM;
            set => SetProperty(ref _outputVM, value);
        }
        ConnectionOutputVM _outputVM;

        public override IReadOnlyList<ConnectionOutputVM> OutputVMs { get; }

        public SingleOutputNodeVM(NodeBase node, QuestVM quest) : base(node, quest)
        {
            _outputVM = new(node.ID, node.NextID);
            OutputVMs = [_outputVM];
        }
    }
}