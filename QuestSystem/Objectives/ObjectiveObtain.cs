using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveObtain : Objective
    {
        public string ItemResRef { get; set; } = string.Empty;
        public string ItemTag { get; set; } = string.Empty;
        public int RequiredAmount { get; set; }

        internal override IObjectiveProgress CreateProgressTrack()
        {
            throw new System.NotImplementedException();
        }

        internal override ObjectiveObtainWrapper Wrap() => new(this);
    }
}