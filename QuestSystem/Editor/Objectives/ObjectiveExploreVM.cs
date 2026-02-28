using QuestEditor.Nodes;
using QuestSystem.Objectives;

namespace QuestEditor.Objectives
{
    internal class ObjectiveExploreVM(ObjectiveExplore model, StageNodeVM parent) : ObjectiveVM(model, parent)
    {
        public override ObjectiveExplore Objective => (ObjectiveExplore)base.Objective;

        public override string ObjectiveType => "Explore";
        public int AreaExplorePercentage
        {
            get => Objective.AreaExplorePercentage;
            set
            {
                if (Objective.AreaExplorePercentage == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.AreaExplorePercentage = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(AreaExplorePercentage), nameof(AreaExplorePercentageString)));
            }
        }
        public string AreaExplorePercentageString
        {
            get => AreaExplorePercentage.ToString();
            set
            {
                if (int.TryParse(value, out var i) && i != AreaExplorePercentage)
                {
                    AreaExplorePercentage = i;
                    RaisePropertyChanged(nameof(AreaExplorePercentageString));
                }
            }
        }
    }
}
