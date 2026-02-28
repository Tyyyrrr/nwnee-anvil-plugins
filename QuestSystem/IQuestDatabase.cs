using Anvil.API;

using QuestSystem.Wrappers.Nodes;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem
{
    internal interface IQuestDatabase
    {
        void LazyLoadPlayerQuests (NwPlayer player);

        void UpdateQuest(NwPlayer player, string questTag);
        void ClearQuest(NwPlayer player, string questTag);

        void UpdateProgress(NwPlayer player, ObjectiveWrapper objective, string questTag, int stageID);
        void ClearProgress(NwPlayer player, int objectiveIndex, string questTag, int stageID);

        void UpdateStageProgress(NwPlayer player, StageNodeWrapper stage);
        void ClearStageProgress(NwPlayer player, string questTag, int stageID);
    }
}