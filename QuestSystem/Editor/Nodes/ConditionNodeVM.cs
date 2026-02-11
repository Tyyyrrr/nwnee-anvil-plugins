using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace QuestEditor.Nodes
{
    public sealed class QuestConditionVM : ViewModelBase
    {
        public QuestConditionVM(QuestCondition condition, ConditionNodeVM parent)
        {
            _condition = (QuestCondition)condition.Clone();
            _stringCondition = _condition.StringCondition;
            _intCondition = _condition.IntCondition;
            _intParameter = _condition.IntParameter;
            _type = _condition.Type;
            _comparison = _condition.Comparison;
            _isInverted = _condition.Invert;

            RemoveConditionCommand = parent.RemoveConditionCommand;

            SetBooleanFlags();
        }
        private QuestCondition _condition;
        public string StringCondition
        {
            get => _stringCondition;
            set {if(SetProperty(ref _stringCondition, value)) _condition.StringCondition = value; Updated?.Invoke(); }
        } private string _stringCondition;

        public int IntCondition
        {
            get => _intCondition;
            set {if (!SetProperty(ref _intCondition, value)) return; _condition.IntCondition = value; Updated?.Invoke();}
        } private int _intCondition;

        public int IntParameter
        {
            get => _intParameter;
            set { if(SetProperty(ref _intParameter, value)) _condition.IntParameter = value; Updated?.Invoke(); }
        } private int _intParameter;

        public QuestCondition.ConditionType Type
        {
            get => _type;
            set { if(SetProperty(ref _type, value)) _condition.Type = value; SetBooleanFlags(); Updated?.Invoke(); }
        } private QuestCondition.ConditionType _type;

        public QuestCondition.ComparisonMode Comparison
        {
            get => _comparison;
            set { if(SetProperty(ref _comparison,value)) _condition.Comparison = value; Updated?.Invoke(); }
        } private QuestCondition.ComparisonMode _comparison;

        public QuestCondition GetCondition() => (QuestCondition)_condition.Clone();
        public void SetCondition(QuestCondition condition)
        {
            _condition = (QuestCondition)condition.Clone();
            _intCondition = _condition.IntCondition;
            RaisePropertyChanged(nameof(IntCondition));
            _intParameter = _condition.IntParameter;
            RaisePropertyChanged(nameof(IntParameter));
            _stringCondition = _condition.StringCondition;
            RaisePropertyChanged(nameof(StringCondition));
            _type = _condition.Type;
            RaisePropertyChanged(nameof(Type));
            _comparison = _condition.Comparison;
            RaisePropertyChanged(nameof(Comparison));
            _isInverted = _condition.Invert;
            RaisePropertyChanged(nameof(IsInverted));
            RaisePropertyChanged(nameof(IntConditionText));
            RaisePropertyChanged(nameof(IntParameterText));

            SetBooleanFlags();
        }

        public void SetBooleanFlags()
        {
            switch (Type)
            {
                case QuestCondition.ConditionType.SkillRoll:
                case QuestCondition.ConditionType.AttributeRoll:
                    NumericConditionVisible = true;
                    NumericParameterVisible = true;
                    TextConditionVisible = false;
                    ComparisonModeVisible = false;
                    break;

                case QuestCondition.ConditionType.SkillRank:
                case QuestCondition.ConditionType.AttributeRank:
                case QuestCondition.ConditionType.ClassLevel:
                    NumericConditionVisible = true;
                    NumericParameterVisible = true;
                    TextConditionVisible = false;
                    ComparisonModeVisible = true;
                    break;


                case QuestCondition.ConditionType.Race:
                    TextConditionVisible = false;
                    NumericConditionVisible = true;
                    NumericParameterVisible = false;
                    ComparisonModeVisible = false;
                    break;

                case QuestCondition.ConditionType.HasTaggedEffect:
                case QuestCondition.ConditionType.Subrace:
                    TextConditionVisible = true;
                    NumericConditionVisible = false;
                    NumericParameterVisible = false;
                    ComparisonModeVisible = false;
                    break;

                case QuestCondition.ConditionType.Level:
                case QuestCondition.ConditionType.AlignmentGoodEvil:
                case QuestCondition.ConditionType.AlignmentLawChaos:
                    TextConditionVisible = false;
                    NumericConditionVisible = true;
                    NumericParameterVisible = false;
                    ComparisonModeVisible = true;
                    break;


                case QuestCondition.ConditionType.HasItem:
                case QuestCondition.ConditionType.CompletedQuest:
                case QuestCondition.ConditionType.OnQuest:
                    TextConditionVisible = true;
                    NumericConditionVisible = true;
                    NumericParameterVisible = false;
                    ComparisonModeVisible = false;
                    break;
            }
        }

        public bool IsInverted
        {
            get => _isInverted;
            set { if(SetProperty(ref _isInverted,value)) _condition.Invert = value; Updated?.Invoke(); }
        }
        bool _isInverted;

        private static IReadOnlyList<QuestCondition.ConditionType> _conditionTypes = Enum.GetValues<QuestCondition.ConditionType>();
        public IReadOnlyList<QuestCondition.ConditionType> ConditionTypes => _conditionTypes;


        private static IReadOnlyList<QuestCondition.ComparisonMode> _comparisonModes = Enum.GetValues<QuestCondition.ComparisonMode>();
        public IReadOnlyList<QuestCondition.ComparisonMode> ComparisonModes => _comparisonModes;

        public ICommand RemoveConditionCommand { get; }





        public bool TextConditionVisible
        {
            get => _textConditionVisible;
            private set => SetProperty(ref _textConditionVisible, value);
        } bool _textConditionVisible;

        public bool NumericConditionVisible
        {
            get => _numericConditionVisible;
            private set => SetProperty(ref _numericConditionVisible, value);
        }
        bool _numericConditionVisible;
        public bool NumericParameterVisible
        {
            get => _numericParameterVisible;
            private set => SetProperty(ref _numericParameterVisible, value);
        }
        bool _numericParameterVisible;
        public bool ComparisonModeVisible
        {
            get => _comparisonModeVisible;
            private set => SetProperty(ref _comparisonModeVisible, value);
        }
        bool _comparisonModeVisible;

        public string IntConditionText
        {
            get => IntCondition.ToString();
            set
            {
                if (!int.TryParse(value, out var i)) return;
                IntCondition = i;
                RaisePropertyChanged(nameof(IntConditionText));
            }
        }

        public string IntParameterText
        {
            get=> IntParameter.ToString();
            set
            {
                if(!int.TryParse(value, out var i)) return;
                IntParameter = i;
                RaisePropertyChanged(nameof(IntParameterText));
            }
        }

        public event Action? Updated;
    }

    public sealed class ConditionNodeVM : NodeVM
    {
        public override string NodeType => "Condition";

        protected override ConditionNode Node => (ConditionNode)base.Node;

        public ObservableCollection<QuestConditionVM> Conditions { get; }
        private QuestCondition[] oldValues;


        public ConnectionOutputVM OutputWhenTrue { get; }
        public ConnectionOutputVM OutputWhenFalse { get; }
        public override IReadOnlyList<ConnectionOutputVM> OutputVMs { get; }

        public ConditionNodeVM(ConditionNode node, QuestVM quest) : base(node, quest)
        {
            Conditions = new ObservableCollection<QuestConditionVM>(node.Conditions.Select(c => new QuestConditionVM(c, this)));
            oldValues = node.Conditions.Select(c => (QuestCondition)c.Clone()).ToArray();
            OutputWhenTrue = new(node.ID, node.NextIDWhenTrue);
            OutputWhenFalse = new(node.ID, node.NextIDWhenFalse);
            OutputVMs = [OutputWhenFalse, OutputWhenTrue];

            OutputWhenFalse.ModeChanged += o => SetNextID(o.TargetID, 0);
            OutputWhenTrue.ModeChanged += o => SetNextID(o.TargetID, 1);
            IsInputAvailable = false;
            IsOutputAvailable = true;

            AddConditionCommand = new RelayCommand(_ => PushOperation(new AddConditionOperation(this)), _ => true);
            RemoveConditionCommand = new RelayCommand(param => PushOperation(new RemoveConditionOperation(this,(QuestConditionVM)param!)), p => p is QuestConditionVM);

            foreach (var c in Conditions)
            {
                c.SetBooleanFlags();
                c.Updated += Update;
            }
        }

        public ICommand AddConditionCommand { get; }
        public ICommand RemoveConditionCommand { get; }



        public override bool CanChangeRollback => true;
        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => null;
        public override bool HasNodeOutput => false;
        protected override void SetNextOutputTargetID(int nextID, int outputIndex)
        {
            if(outputIndex == 0)
                OutputWhenFalse.TargetID = nextID;
            else OutputWhenTrue.TargetID = nextID;
        }

        protected override void Apply()
        {
            Node.Conditions = [.. Conditions.Select(c => c.GetCondition())];

            //Trace.WriteLine("Writing new target id: true:" + OutputWhenTrue.TargetID + " false: " + OutputWhenFalse.TargetID);
            Node.NextIDWhenTrue = OutputWhenTrue.TargetID;
            Node.NextIDWhenFalse = OutputWhenFalse.TargetID;
            oldValues = Node.Conditions.Select(c => (QuestCondition) c.Clone()).ToArray();
            base.Apply();
        }

        private sealed class AddConditionOperation(ConditionNodeVM origin) : UndoableOperation(origin)
        {
            QuestConditionVM? vm;
            protected override void ProtectedDo()
            {
                var condition = new QuestCondition();
                vm = new QuestConditionVM(condition, origin);
                ProtectedRedo();
            }

            protected override void ProtectedRedo()
            {
                vm!.Updated += origin.Update;
                origin.Conditions.Add(vm);
                origin.oldValues = origin.Conditions.Select(c => c.GetCondition()).ToArray();
            }
            protected override void ProtectedUndo()
            {
                vm!.Updated -= origin.Update;
                origin.Conditions.Remove(vm);
                origin.oldValues = origin.Conditions.Select(c => c.GetCondition()).ToArray();
            }
        }

        private sealed class RemoveConditionOperation(ConditionNodeVM origin, QuestConditionVM condition) : UndoableOperation(origin)
        {
            protected override void ProtectedDo()
            {
                condition.Updated -= origin.Update;
                origin.Conditions.Remove(condition);
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                condition.Updated += origin.Update;
                origin.Conditions.Add(condition);
            }
        }

        bool pushLock = false;
        void Update()
        {
            if (pushLock) return;

            PushOperation(new UpdateConditionsOperation(this));
        }

        private sealed class UpdateConditionsOperation(ConditionNodeVM origin) : UndoableOperation(origin)
        {
            QuestCondition[] oldValues = origin.oldValues.Select(o=>(QuestCondition)o.Clone()).ToArray();
            QuestCondition[] newValues = origin.Conditions.Select(c=>c.GetCondition()).ToArray();
            protected override void ProtectedDo()
            {
                origin.oldValues = newValues.Select(o=>(QuestCondition)o.Clone()).ToArray();
            }

            protected override void ProtectedUndo()
            {
                origin.oldValues = oldValues.Select(o => (QuestCondition)o.Clone()).ToArray();

                origin.pushLock = true;

                for (int i = 0; i < oldValues.Length; i++)
                {
                    origin.Conditions[i].SetCondition(oldValues[i]);
                }
                origin.pushLock = false;
            }

            protected override void ProtectedRedo()
            {
                ProtectedDo();

                origin.pushLock = true;

                for(int i = 0; i < newValues.Length; i++)
                {
                    origin.Conditions[i].SetCondition(newValues[i]);
                }

                origin.pushLock = false;
            }
        }
    }
}
