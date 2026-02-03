using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace QuestEditor.Nodes
{

    public sealed class RandomizerNodeVM : NodeVM
    {
        public sealed class RandomizerNodeElementVM : ViewModelBase
        {
            public string ChanceString => $"{SliderValue:N}%";
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
                Output = new(parent.ID, targetID);
                chance = Math.Clamp(chance, 0, 100);
                SliderValue = chance;

                DeleteBranchCommand = new RelayCommand(parent.RemoveBranch, _ => true);
            }
            public ICommand DeleteBranchCommand { get; }
        }



        public void TryPushUndoableChanges()
        {
            for(int i = 0; i < Elements.Count; i++)
            {
                var element = Elements[i];
                if (!Node.Branches.TryGetValue(element.Output.TargetID, out var val) || val != element.SliderValue) 
                {
                    var backup = (RandomizerNode)Node.Clone();
                    var branches = Node.Branches;
                    branches.Clear();
                    foreach (var e in Elements)
                    {
                        _ = branches.TryAdd(e.Output.TargetID, e.SliderValue);
                    }
                    PushOperation(new UpdateBranchesOperation(this, backup.Branches.Select(kvp=>(kvp.Key,kvp.Value)).ToArray(), Node.Branches.Select(kvp=>(kvp.Key,kvp.Value)).ToArray()));
                    return;
                }
            }
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
                elem.PropertyChanged += OnElementPropertyChanged;
            }

            IsOutputAvailable = true;
            AddBranchCommand = new RelayCommand(_ => PushOperation(new AddBranchOperation(this)), _ => true);
        }

        protected override void SetNextOutputTargetID(int nextID, int outputIndex)
        {
            bool swapped = false;
            for(int i = 0; i <  Elements.Count; i++)
            {
                var output = Elements[i].Output;
                if (output.TargetID == nextID && outputIndex != i)
                {
                    output.TargetID = -1;
                    var conn = Elements[i].Output.Connections.FirstOrDefault();
                    if(conn != null)
                    {
                        output.Connections.Clear();
                        conn.Input?.Connections.Remove(conn);
                        conn.Input = null;
                        conn.Output = null;
                    }
                    swapped = true;
                }
            }
            if (swapped) RaiseShouldReconnectAllNodes();
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
                origin.Elements.Add(_element);
                _element.PropertyChanged += origin.OnElementPropertyChanged;
                RaiseShouldReconnectAllNodes();
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                origin.Elements.Remove(_element);
                _element.PropertyChanged -= origin.OnElementPropertyChanged;
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
                _element.PropertyChanged -= origin.OnElementPropertyChanged;
                RaiseShouldReconnectAllNodes();
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                var vm = (RandomizerNodeVM)Origin;
                vm.Elements.Add(_element);
                _element.PropertyChanged += origin.OnElementPropertyChanged;
                RaiseShouldReconnectAllNodes();
            }
        }

        bool lockProperties = false;
        void OnElementPropertyChanged(object? s, PropertyChangedEventArgs args)
        {
            if (lockProperties || s is not RandomizerNodeElementVM element || args.PropertyName != nameof(RandomizerNodeElementVM.SliderValue))
                return;

            lockProperties = true;

            IEnumerable<RandomizerNodeElementVM> elementsToUpdate = Elements.Where(e => e != element);

            float[] values = [.. elementsToUpdate.Select(e => e.SliderValue)];

            values = CoerceToTargetSum(values, 100f - element.SliderValue);

            int i = 0;
            foreach(var e in elementsToUpdate)
                e.SliderValue = values[i++];

            lockProperties = false;
        }

        static float[] CoerceToTargetSum(float[] values, float targetSum)
        {
            double[] dValues = values.Select(v => (double)v).ToArray();

            double sum = values.Sum();

            if (sum == targetSum) return values;

            if(targetSum == 0)
            {
                return new float[values.Length];
            }

            if(sum == 0)
            {
                var res = new float[values.Length];
                Array.Fill(res, (float)((double)targetSum / values.Length));
                return res;
            }

            double diff = sum - targetSum;

            double mul = 1f - (diff / targetSum);

            var result = new float[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                result[i] = (float)(values[i] * mul);
            }

            var finalSum = result.Sum();

            Trace.WriteLineIf(finalSum != targetSum, $"Final sum: {finalSum}, target sum: {targetSum}");


            return result;
        }

        private sealed class UpdateBranchesOperation(RandomizerNodeVM origin, (int, float)[] before, (int, float)[] after) : UndoableOperation(origin)
        {
            protected override void ProtectedDo()
            {
                origin.Node.Branches.Clear();
                for(int i = 0; i < after.Length; i++)
                {
                    origin.Node.Branches.TryAdd(after[i].Item1, after[i].Item2);
                    var elem = origin.Elements.FirstOrDefault(e => e.Output.TargetID == after[i].Item1);
                    if (elem != null)
                        elem.SliderValue = after[i].Item2;
                    
                }
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                origin.Node.Branches.Clear();
                for (int i = 0; i < before.Length; i++)
                {
                    origin.Node.Branches.TryAdd(before[i].Item1, before[i].Item2);
                    var elem = origin.Elements.FirstOrDefault(e => e.Output.TargetID == before[i].Item1);
                    if (elem != null)
                        elem.SliderValue = before[i].Item2;
                    
                }
            }
        }
    }
}
