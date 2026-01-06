using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using QuestEditor.PropertyList;
using QuestEditor.Shared;
using QuestSystem;
using QuestSystem.Objectives;

namespace QuestEditor.ObjectiveBox;

public sealed class ObjectiveBoxViewModel : ViewModelBase
{
    public PropertyListViewModel Objective {get;}
    public PropertyListViewModel Reward {get;}

    public ObservableCollection<string> AreaTags {get;}=[];
    public ICommand AddAreaTagCommand {get;}
    public ICommand RemoveAreaTagCommand {get;}
    public ObjectiveBoxViewModel(Objective objective)
    {
        Objective = new(objective){Header = objective.GetType().Name};
        Reward = new(objective.Reward) {Header = "Reward"};
        AreaTags=new(objective.AreaTags);
        AddAreaTagCommand = new RelayCommand(AddAreaTag);
        RemoveAreaTagCommand = new RelayCommand(RemoveAreaTag);
    }

    void AddAreaTag(object? param)
    {
        if(param is null or not string) return;
        AreaTags.Add((string)param);
    }

    void RemoveAreaTag(object? param)
    {
        if(param is null or not string) return;
        AreaTags.Remove((string)param);
    }

    public Objective? GetQuestObjective()
    {
        var objective = Objective.GetT<Objective>();
        var reward = Reward.GetT<QuestStageReward>();

        if(objective != null)
        {
            objective.Reward = reward ?? new();
            objective.AreaTags = [.. AreaTags];
        }
        Console.WriteLine($"Got objective out from objectiveBox. Type: {objective?.GetType().Name ?? "<NULL>"}");
        return objective;
    }
}