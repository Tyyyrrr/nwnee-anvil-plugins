using QuestSystem.Wrappers;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveDeliver : Objective
    {
        public string ItemResRef { get; set; } = string.Empty;
        public string ItemTag { get; set; } = string.Empty;
        public int RequiredAmount { get; set; }
        public bool AllowPartialDelivery { get; set; }
        public bool DestroyItemsOnDelivery { get; set; }
        internal override ObjectiveDeliverWrapper Wrap() => new(this);
        internal override IObjectiveProgress CreateProgressTrack()
        {
            throw new System.NotImplementedException();
        }
    }
}