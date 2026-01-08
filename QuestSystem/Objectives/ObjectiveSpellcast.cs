using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveSpellcast : Objective
    {
        public string TargetResRef { get; set; } = string.Empty;
        public string TargetTag { get; set; } = string.Empty;
        public int SpellID { get; set; } = -1;

        internal override IObjectiveProgress CreateProgressTrack()
        {
            throw new System.NotImplementedException();
        }

        internal override ObjectiveSpellcastWrapper Wrap() => new(this);
    }
}