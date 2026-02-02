using QuestEditor.Nodes;
using QuestSystem.Objectives;

namespace QuestEditor.Objectives
{
    internal class ObjectiveInteractVM(ObjectiveInteract model, StageNodeVM parent) : ObjectiveVM(model, parent)
    {
        private static IReadOnlyList<ObjectiveInteract.InteractionType> _interactionTypes = Enum.GetValues<ObjectiveInteract.InteractionType>();
        public IReadOnlyList<ObjectiveInteract.InteractionType> InteractionTypes => _interactionTypes;

        public override ObjectiveInteract Objective => (ObjectiveInteract)base.Objective;

        public override string ObjectiveType => "Interact";
        public ObjectiveInteract.InteractionType Interaction
        {
            get => Objective.Interaction;
            set
            {
                if (Objective.Interaction == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Interaction = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(Interaction)));
            }
        }

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
                if (Objective.Tag == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Tag = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(Tag)));
            }
        }
    }
}
