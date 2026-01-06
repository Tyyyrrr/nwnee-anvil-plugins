using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using QuestEditor.PropertyList;
using QuestEditor.Shared;
using QuestSystem;

namespace QuestEditor.RewardBox
{
    public sealed class RewardBoxViewModel : ViewModelBase
    {
        public PropertyListViewModel Reward {get;}
        public ObservableCollection<PropertyListViewModel> Items {get;}
        public ObservableCollection<PropertyListViewModel> ObjectVisibility {get;}

        public ICommand AddItemResRefCommand {get;}
        public ICommand RemoveItemResRefCommand {get;}
        public ICommand AddObjectTagCommand {get;}
        public ICommand RemoveObjectTagCommand {get;}

        private sealed class ItemsListElement(string resRef)
        {
            public string ResRef {get;} = resRef;
            public int Amount {get;set;} = 0;
        }
        private sealed class VisibilityListElement(string tag)
        {
            public string ObjectTag {get;} = tag;
            public bool Visible {get;set;} = false;
        }
        public QuestStageReward? GetQuestStageReward()
        {
            var reward = Reward.GetT<QuestStageReward>();
            if(reward == null) return null;
            reward.Items = Items.Select(vm=>vm.GetT<ItemsListElement>())
                .Where(e=>e != null)
                .Select(e=>new KeyValuePair<string,int>(e!.ResRef,e.Amount))
                .ToDictionary();

            reward.ObjectVisibility = ObjectVisibility.Select(vm=>vm.GetT<VisibilityListElement>())
                .Where(e=>e != null)
                .Select(e=>new KeyValuePair<string, bool>(e!.ObjectTag,e.Visible))
                .ToDictionary();

            return reward;
        }

        public RewardBoxViewModel(QuestStageReward model)
        {
            Reward = new(model){Header = "Basic"};
            Items = new(model.Items.Select(kvp=>new PropertyListViewModel(new ItemsListElement(kvp.Key){Amount=kvp.Value}){Header=kvp.Key}));
            ObjectVisibility=new(model.ObjectVisibility.Select(kvp=>new PropertyListViewModel(new VisibilityListElement(kvp.Key){Visible=kvp.Value}){Header=kvp.Key}));

            AddItemResRefCommand = new RelayCommand(AddItemResRef,CanAddItemResRef);
            RemoveItemResRefCommand = new RelayCommand(RemoveItemResRef,CanRemoveItemResRef);

            AddObjectTagCommand = new RelayCommand(AddObjectTag,CanAddObjectTag);
            RemoveObjectTagCommand = new RelayCommand(RemoveObjectTag,CanRemoveObjectTag);
        }

        private string? _itemResRefToAdd = string.Empty;
        public string? ItemResRefToAdd
        {
            get => _itemResRefToAdd;
            set
            {
                if(_itemResRefToAdd == value) return;
                _itemResRefToAdd = value;
                OnPropertyChanged(nameof(ItemResRefToAdd));
            }
        }
        void AddItemResRef(object? param)
        {
            Console.WriteLine("Adding item " + (string)param!);
            var elem = new ItemsListElement((string)param!){Amount=1};
            var vm = new PropertyListViewModel(elem){Header=elem.ResRef};
            Items.Add(vm);
            (AddItemResRefCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        bool CanAddItemResRef(object? param)
        {
            if(param is null or not string) return false;
            var str = (string)param;
            return !(string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str)) && !Items.Any(vm=>vm.Header==str);
        }

        void RemoveItemResRef(object? param)
        {
            var vm = (PropertyListViewModel)param!;
            _ = Items.Remove(vm);
            (AddItemResRefCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        bool CanRemoveItemResRef(object? param)
        {
            return !(param is null or not PropertyListViewModel);
        }

        private string? _objectTagToAdd = string.Empty;
        public string? ObjectTagToAdd
        {
            get => _objectTagToAdd;
            set
            {
                if(_objectTagToAdd == value) return;
                _objectTagToAdd = value;
                OnPropertyChanged(nameof(ObjectTagToAdd));
            }
        }

        void AddObjectTag(object? param)
        {
            Console.WriteLine("Adding visibility override for" + (string)param!);
            var elem = new VisibilityListElement((string)param!){Visible=true};
            var vm = new PropertyListViewModel(elem){Header = elem.ObjectTag};
            ObjectVisibility.Add(vm);
            (AddObjectTagCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        bool CanAddObjectTag(object? param)
        {
            if(param is null or not string) return false;
            var str = (string)param;
            return !(string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str)) && !ObjectVisibility.Any(vm=>vm.Header==str);
        }

        void RemoveObjectTag(object? param)
        {
            var vm = (PropertyListViewModel)param!;
            _ = ObjectVisibility.Remove(vm);
            (AddObjectTagCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        bool CanRemoveObjectTag(object? param)
        {
            return !(param is null or not PropertyListViewModel);
        }
    }
}