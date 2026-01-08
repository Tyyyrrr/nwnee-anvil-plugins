using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveExplore : Objective
    {
        public int AreaExplorePercentage { get; set; }

        internal override IObjectiveProgress CreateProgressTrack()
        {
            throw new System.NotImplementedException();
        }

        internal override ObjectiveExploreWrapper Wrap() => new(this);
    }
}