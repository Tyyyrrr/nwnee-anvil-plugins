using QuestEditor.Nodes;
using QuestSystem.Objectives;

namespace QuestEditor.Objectives
{
    internal class ObjectiveObtainVM(ObjectiveObtain model, StageNodeVM parent) : ObjectiveVM(model, parent)
    {
        public override ObjectiveObtain Objective => (ObjectiveObtain)base.Objective;

        public string ItemResRef
        {
            get => Objective.ItemResRef;
            set
            {
                if (Objective.ItemResRef == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.ItemResRef = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(ItemResRef)));
            }
        }

        public string ItemTag
        {
            get => Objective.ItemTag;
            set
            {
                if (Objective.ItemTag == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.ItemTag = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(ItemTag)));
            }
        }

        public int RequiredAmount
        {
            get => Objective.RequiredAmount;
            set
            {
                if (Objective.RequiredAmount == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.RequiredAmount = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(RequiredAmount)));
            }
        }
    }
}
