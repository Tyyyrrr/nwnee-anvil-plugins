using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Objectives;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using QuestSystem.Objectives;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

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
            _inputVM.SocketColorBrush = (SolidColorBrush)((App)Application.Current).Resources["RegularNodeInputSocketBrush"];
            clone = (NodeBase)node.Clone();
            DeleteNodeCommand = new RelayCommand(quest.RemoveNode, _=>true);
        }
        public static NodeVM? SelectViewModel(NodeBase node, QuestVM quest)
        {
            if (node is StageNode stageNode) return new StageNodeVM(stageNode, quest);
            else if (node is RewardNode rewardNode) return new RewardNodeVM(rewardNode, quest);
            else if (node is RandomizerNode randomizerNode) return new RandomizerNodeVM(randomizerNode, quest);
            else if (node is CooldownNode cooldownNode) return new CooldownNodeVM(cooldownNode, quest);
            else if (node is VisibilityNode visibilityNode) return new VisibilityNodeVM(visibilityNode, quest);
            else if (node is ConditionNode conditionNode) return new ConditionNodeVM(conditionNode, quest);
            else return null;
        }

        public ICommand DeleteNodeCommand { get; }

        protected virtual NodeBase Node => clone;

        public virtual bool HasNodeOutput { get; } = true;

        public int ID => Model.ID;
        public virtual bool CanChangeRollback => true;
        public abstract string NodeType { get; }
        public string NodeTitle => NodeType;


        public event Action<NodeVM, (int, int)>? OutputChanged;
        public static event Action? ShouldReconnectAllNodes;
        protected static void RaiseShouldReconnectAllNodes() => ShouldReconnectAllNodes?.Invoke();
        protected void RaiseOutputChanged(int outputIndex, int targetNodeID)
        {
            OutputChanged?.Invoke(this,(outputIndex, targetNodeID));
        }
        public int NextID
        {
            get => Model.NextID;
            set
            {
                if(Model.NextID == value) return;

                Model.NextID = value;
                OutputVMs[0].TargetID = value;
                RaisePropertyChanged(nameof(NextID));
                RaisePropertyChanged(nameof(NextIDString));
                RaiseOutputChanged(0, NextID);
            }

        }


        bool pushLock = false;
        public void SetNextID(int nextID, int outputIndex = 0)
        {
            if (pushLock) return;

            //Trace.WriteLine(Node.ID.ToString() + " setting next ID of output " + outputIndex + " to " + nextID);
            if(outputIndex > 0 || (!this.HasNodeOutput && outputIndex == 0))
            {
                var output = OutputVMs[outputIndex];
                //if (output.TargetID == nextID) return;
                PushOperation(new SetOuptutTargetIDOperation(this, output, nextID));
                return;
            }
            
            if (nextID != NextID)
                PushOperation(new SetNextIDOperation(this, nextID));
        }

        protected virtual void SetNextOutputTargetID(int nextID, int outputIndex) { }


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

        protected sealed class SetOuptutTargetIDOperation(NodeVM origin, ConnectionOutputVM output, int newVal) : UndoableOperation(origin)
        {
            private readonly ConnectionOutputVM _output = output;
            private readonly int _oldVal = output.TargetID;
            private readonly int _newVal = newVal;
            protected override void ProtectedDo()
            {
                for(int i = 0; i < origin.OutputVMs.Count; i++)
                {
                    if (origin.OutputVMs[i] == _output)
                    {
                        origin.pushLock = true;
                        _output.TargetID = _newVal;
                        origin.SetNextOutputTargetID(_newVal, i);
                        origin.RaiseOutputChanged(i, _newVal);
                        origin.pushLock = false;
                        return;
                    }
                }
            }

            protected override void ProtectedUndo()
            {
                for (int i = 0; i < origin.OutputVMs.Count; i++)
                {
                    if (origin.OutputVMs[i] == _output)
                    {
                        origin.pushLock = true;
                        _output.TargetID = _oldVal;
                        origin.SetNextOutputTargetID(_oldVal, i);
                        origin.RaiseOutputChanged(i, _oldVal);
                        origin.pushLock = false;
                        return;
                    }
                }
            }
            protected override void ProtectedRedo() => ProtectedDo();
        }
        protected sealed class SetNextIDOperation(NodeVM node, int newVal) : UndoableOperation(node)
        {
            private readonly int _oldVal = node.NextID;
            private readonly int _newVal = newVal;
            protected override void ProtectedDo()
            {
                node.pushLock = true;
                node.NextID = _newVal;
                node.pushLock = false;
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                node.pushLock = true;
                node.NextID = _oldVal;
                node.pushLock = false;
            }
        }

        private sealed class SetRollbackOperation(NodeVM node, bool rollback) : UndoableOperation(node)
        {
            private bool _oldVal = node.Rollback;
            private bool _rollback = rollback;

            protected override void ProtectedDo()
            {
                node.pushLock = true;
                node.Model.Rollback = _rollback;
                node.RaisePropertyChanged(nameof(Rollback));
                node.pushLock = false;
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                node.pushLock = true;
                node.Model.Rollback = _oldVal;
                node.RaisePropertyChanged(nameof(Rollback));
                node.pushLock = false;
            }
        }

        protected sealed class UpdateNodeOperation(NodeVM origin, NodeBase before, NodeBase after, string propertyName) : UndoableOperation(origin)
        {
            readonly NodeBase _before = before;
            readonly NodeBase _after = after;
            readonly string _propertyName = propertyName;

            protected override void ProtectedDo() { }

            protected override void ProtectedRedo()
            {
                origin.pushLock = true;
                origin.clone = _after;
                origin.RaisePropertyChanged(_propertyName);
                origin.pushLock = false;
            }

            protected override void ProtectedUndo()
            {
                origin.pushLock = true;
                origin.clone = _before;
                origin.RaisePropertyChanged(_propertyName);
                origin.pushLock = false;
            }
        }

        public bool IsSelected { get => _isSelected; private set => SetProperty(ref _isSelected, value); }
        private bool _isSelected = false;
        public void Select()
        {
            //Trace.WriteLine("Node select");
        }

        public void ClearSelection()
        {
            //Trace.WriteLine("Node clear selection");
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
            _outputVM.ModeChanged += o => SetNextID(o.TargetID, 0);
            IsInputAvailable = false;
            IsOutputAvailable = true;
        }

    }
}