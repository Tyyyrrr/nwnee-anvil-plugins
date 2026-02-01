using QuestEditor.Nodes;
using QuestSystem.Objectives;

namespace QuestEditor.Objectives
{
    internal class ObjectiveDeliverVM(ObjectiveDeliver model, StageNodeVM parent) : ObjectiveVM(model, parent)
    {
        public override ObjectiveDeliver Objective => (ObjectiveDeliver)base.Objective;

        public override string ObjectiveType => "Deliver";
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

        public bool AllowPartialDelivery
        {
            get => Objective.AllowPartialDelivery;
            set
            {
                if (Objective.AllowPartialDelivery == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.AllowPartialDelivery = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(AllowPartialDelivery)));
            }
        }
        public bool DestroyItemsOnDelivery
        {
            get => Objective.DestroyItemsOnDelivery;
            set
            {
                if (Objective.DestroyItemsOnDelivery == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.DestroyItemsOnDelivery = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(DestroyItemsOnDelivery)));
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
