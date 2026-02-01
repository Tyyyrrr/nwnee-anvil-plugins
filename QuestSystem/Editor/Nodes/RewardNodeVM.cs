using QuestEditor.Explorer;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuestEditor.Nodes
{
    public sealed class RewardNodeVM : SingleOutputNodeVM
    {
        protected override RewardNode Node => (RewardNode)base.Node;
        public override string NodeType => "Reward";

        public string GoldString
        {
            get => Node.Gold.ToString();
            set
            {
                if (!int.TryParse(value, out var i) || i == Node.Gold) return;
                var before = (RewardNode)Node.Clone();
                Node.Gold = i;
                PushOperation(new UpdateNodeOperation(this, before, Node, nameof(GoldString)));
            }
        }

        public string XpString
        {
            get => Node.Xp.ToString();
            set
            {
                if(!int.TryParse(value, out var i) && i == Node.Xp) return;
                var before = (RewardNode)Node.Clone();
                Node.Xp = i;
                PushOperation(new UpdateNodeOperation(this,before, Node, nameof(XpString)));
            }
        }

        public string AlignmentGEString
        {
            get => Node.GoodEvilChange.ToString();
            set
            {
                if (!int.TryParse(value, out var i) && i == Node.Xp) return;
                var before = (RewardNode)Node.Clone();
                Node.GoodEvilChange = i;
                PushOperation(new UpdateNodeOperation(this, before, Node, nameof(AlignmentGEString)));
            }
        }
        public string AlignmentLCString
        {
            get => Node.LawChaosChange.ToString();
            set
            {
                if (!int.TryParse(value, out var i) && i == Node.Xp) return;
                var before = (RewardNode)Node.Clone();
                Node.LawChaosChange = i;
                PushOperation(new UpdateNodeOperation(this, before, Node, nameof(AlignmentLCString)));
            }
        }

        public string NewItemString
        {
            get => _newItemString;
            set
            {
                if (SetProperty(ref _newItemString, value))
                    ((RelayCommand)AddItemCommand).RaiseCanExecuteChanged();
            }
        } string _newItemString = string.Empty;

        public string NewItemAmount
        {
            get => _newItemAmount;
            set
            {
                if (!int.TryParse(value, out var i) && SetProperty(ref _newItemAmount, 0.ToString()))
                {
                    ((RelayCommand)AddItemCommand).RaiseCanExecuteChanged();
                    return;
                }
                if(SetProperty(ref _newItemAmount, value))
                    ((RelayCommand)AddItemCommand).RaiseCanExecuteChanged();
            }
        } string _newItemAmount = 0.ToString();


        bool CanAddNewItem(object? _) => !string.IsNullOrWhiteSpace(_newItemString) && !string.IsNullOrEmpty(_newItemString) && int.TryParse(_newItemAmount, out var i) && i > 0;
        

        public RewardNodeVM(RewardNode node, QuestVM quest) : base(node, quest)
        {
            AddItemCommand = new RelayCommand(AddItem, CanAddNewItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, _ => true);

            foreach(var kvp in node.Items)
            {
                var item = new StringIntListItem(kvp.Key, kvp.Value);
                Items.Add(item);
            }
            IsInputAvailable = false;
            IsOutputAvailable = true;
        }

        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }

        void AddItem(object? _)
        {
            var item = new StringIntListItem { StringValue = NewItemString, IntValue = int.Parse(NewItemAmount) };
            PushOperation(new AddItemOperation(this, item));
        }

        void RemoveItem(object? item)
        {
            if (item is not StringIntListItem sili || !Node.Items.ContainsKey(sili.StringValue)) 
                return;
            PushOperation(new RemoveItemOperation(this, sili));
        }

        public ObservableCollection<StringIntListItem> Items { get; private set; } = [];

        private sealed class AddItemOperation(RewardNodeVM origin, StringIntListItem item) : UndoableOperation(origin)
        {
            private readonly StringIntListItem _item = item;
            protected override void ProtectedDo() => ProtectedRedo();
            protected override void ProtectedRedo()
            {
                var vm = (RewardNodeVM)Origin;
                if(!vm.Node.Items.TryAdd(_item.StringValue,_item.IntValue))
                    vm.Node.Items[_item.StringValue] = _item.IntValue;
                vm.Items.Add(_item);
            }

            protected override void ProtectedUndo()
            {
                var vm = (RewardNodeVM)Origin;
                _ = vm.Node.Items.Remove(_item.StringValue);
                _ = vm.Items.Remove(_item);
            }
        }
        private sealed class RemoveItemOperation(RewardNodeVM origin, StringIntListItem item) : UndoableOperation(origin)
        {
            private readonly StringIntListItem _item = item;
            protected override void ProtectedDo() => ProtectedRedo();
            protected override void ProtectedRedo()
            {

                var vm = (RewardNodeVM)Origin;
                _ = vm.Node.Items.Remove(_item.StringValue);
                _ = vm.Items.Remove(_item);

            }

            protected override void ProtectedUndo()
            {
                var vm = (RewardNodeVM)Origin;
                if (!vm.Node.Items.TryAdd(_item.StringValue, _item.IntValue))
                    vm.Node.Items[_item.StringValue] = _item.IntValue;
                vm.Items.Add(_item);
            }
        }

    }
}