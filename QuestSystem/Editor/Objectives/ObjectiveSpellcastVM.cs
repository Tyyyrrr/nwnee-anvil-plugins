using QuestEditor.Nodes;
using QuestSystem.Objectives;

namespace QuestEditor.Objectives
{
    internal class ObjectiveSpellcastVM(ObjectiveSpellcast model, StageNodeVM parent) : ObjectiveVM(model, parent)
    {
        public override ObjectiveSpellcast Objective => (ObjectiveSpellcast)base.Objective;

        public override string ObjectiveType => "Spellcast";

        public int SpellID
        {
            get => Objective.SpellID;
            set
            {
                if (Objective.SpellID == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.SpellID = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(SpellID), nameof(SpellIDString)));
            }
        }

        public string SpellIDString
        {
            get => SpellID.ToString();
            set
            {
                if (int.TryParse(value, out var i) && i != SpellID)
                {
                    SpellID = i;
                    RaisePropertyChanged(nameof(SpellIDString));
                }
            }
        }

        public string TargetResRef
        {
            get => Objective.TargetResRef;
            set
            {
                if (Objective.TargetResRef == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.TargetResRef = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(TargetResRef)));
            }
        }

        public string TargetTag
        {
            get => Objective.TargetTag;
            set
            {
                if (Objective.TargetTag == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.TargetTag = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(TargetTag)));
            }
        }
    }
}
