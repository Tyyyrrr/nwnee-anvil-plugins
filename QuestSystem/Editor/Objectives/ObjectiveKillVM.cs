using QuestEditor.Nodes;
using QuestSystem.Objectives;

namespace QuestEditor.Objectives
{
    internal class ObjectiveKillVM(ObjectiveKill model, StageNodeVM parent) : ObjectiveVM(model, parent)
    {
        public override ObjectiveKill Objective => (ObjectiveKill)base.Objective;

        public override string ObjectiveType => "Kill";
        public string ResRef
        {
            get => Objective.ResRef;
            set
            {
                if (Objective.ResRef == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.ResRef = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(ResRef)));
            }
        }

        public string Tag
        {
            get => Objective.Tag;
            set
            {
                if (Objective.ResRef == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Tag = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(Tag)));
            }
        }

        public int Amount
        {
            get => Objective.Amount;
            set
            {
                if (Objective.Amount == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Amount = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(Amount)));
            }
        }
    }
}
