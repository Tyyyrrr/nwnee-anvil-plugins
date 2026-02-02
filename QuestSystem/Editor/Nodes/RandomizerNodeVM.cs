using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace QuestEditor.Nodes
{

    public sealed class RandomizerNodeVM : NodeVM
    {
        public sealed class RandomizerNodeElementVM : ViewModelBase
        {
            public string ChanceString => SliderValue.ToString("N");

            public float SliderValue
            {
                get => _sliderValue;
                set
                {
                    if (SetProperty(ref _sliderValue, value))
                        RaisePropertyChanged(nameof(ChanceString));

                }
            } float _sliderValue;

            public ConnectionOutputVM Output { get; }

            public RandomizerNodeElementVM(RandomizerNodeVM parent, int targetID, float chance)
            {
                Output = new(parent.ID, -1);
                chance = Math.Clamp(chance, 0, 100);
                SliderValue = chance;

                DeleteBranchCommand = new RelayCommand(parent.RemoveBranch, _ => true);
            }
            public ICommand DeleteBranchCommand { get; }
        }



        protected override void Apply()
        {
            var branches = Node.Branches;
            branches.Clear();
            foreach(var element in Elements)
            {
                _ = branches.TryAdd(element.Output.TargetID, element.SliderValue);
            }
            base.Apply();
        }


        public RandomizerNodeVM(RandomizerNode node, QuestVM quest) : base(node, quest)
        {
            foreach(var kvp in node.Branches)
            {
                var elem = new RandomizerNodeElementVM(this, kvp.Key, kvp.Value);
                Elements.Add(elem);
            }

            IsOutputAvailable = true;
            AddBranchCommand = new RelayCommand(_ => PushOperation(new AddBranchOperation(this)), _ => true);
        }


        public override bool HasNodeOutput => false;



        protected override RandomizerNode Node => (RandomizerNode)base.Node;
        public override string NodeType => "Randomizer";

        public override IReadOnlyList<ConnectionOutputVM> OutputVMs => [.. Elements.Select(e => e.Output)];

        public ObservableCollection<RandomizerNodeElementVM> Elements { get; } = [];
    
    
    
        public ICommand AddBranchCommand { get; }
        
    
        void RemoveBranch(object? parameter)
        {
            var elem = parameter as RandomizerNodeElementVM;
            if (elem != null)
                PushOperation(new RemoveBranchOperation(this, elem));
            
        }

        private sealed class AddBranchOperation(RandomizerNodeVM origin) : UndoableOperation(origin)
        {
            private readonly RandomizerNodeElementVM _element = new(origin, -1, 0);
            protected override void ProtectedDo()
            {
                var vm = (RandomizerNodeVM)Origin;
                vm.Elements.Add(_element);
                RaiseShouldReconnectAllNodes();
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                var vm = (RandomizerNodeVM)Origin;
                vm.Elements.Remove(_element);
                RaiseShouldReconnectAllNodes();
            }
        }

        private sealed class RemoveBranchOperation(RandomizerNodeVM origin, RandomizerNodeElementVM element) : UndoableOperation(origin)
        {
            private readonly RandomizerNodeElementVM _element = element;
            protected override void ProtectedDo()
            {
                var vm = (RandomizerNodeVM)Origin;
                vm.Elements.Remove(_element);
                RaiseShouldReconnectAllNodes();
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                var vm = (RandomizerNodeVM)Origin;
                vm.Elements.Add(_element);
                RaiseShouldReconnectAllNodes();
            }
        }

    }
}
