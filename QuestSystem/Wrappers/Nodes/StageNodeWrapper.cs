using System.Linq;

using Anvil.API;

using QuestSystem.Nodes;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class StageNodeWrapper : NodeWrapper<StageNode>
    {
        public override bool IsRoot => true;

        public bool ShowInJournal => Node.ShowInJournal;

        public string? GetStageJournalEntry(NwPlayer player)
        {
            if(!ShowInJournal) return string.Empty;

                //todo: add regexp for macros in the text

            return Node.JournalEntry;
        }
        public string? GetObjectivesJournalEntry(NwPlayer player)
        {
            if(!ShowInJournal || _objectives.Length == 0) return string.Empty;

            string text = string.Empty;

            foreach(var obj in _objectives.Where(o=>o.ShowInJournal))
                text += obj.GetJournalText(player);

            return text;
        }

        private readonly ObjectiveWrapper[] _objectives;

        public StageNodeWrapper(StageNode questStage) : base(questStage)
        {
            _objectives = new ObjectiveWrapper[questStage.Objectives.Length];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = (ObjectiveWrapper)questStage.Objectives[i].Wrap();
                _objectives[i].Updated += OnObjectiveUpdated;
            }
        }

        void OnObjectiveUpdated(ObjectiveWrapper wrapper, NwPlayer player) => RaiseShouldEvaluate(player);

        public override void Enter(NwPlayer player)
        {
            foreach(var objective in _objectives)
                objective.StartTrackingProgress(player);
        }

        public override void Reset(NwPlayer player)
        {
            foreach(var objective in _objectives)
                objective.StopTrackingProgress(player);
        }

        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            bool allCompleted = true;
            foreach(var objective in _objectives)
            {
                if(objective.NextID != -1)
                {
                    if(!objective.IsCompleted(player))
                        continue;

                    nextId = objective.NextID;
                    return true;
                }
                else if (!objective.IsCompleted(player))
                {
                    allCompleted = false;
                }
            }
            nextId = NextID;
            return allCompleted;
        }        
        
        protected override void ProtectedDispose()
        {
            foreach(var objective in _objectives)
                objective.Dispose();
        }

    }
}