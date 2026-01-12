using Anvil.API;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    internal interface IQuestDatabase
    {
        void LazyLoadPlayerQuests (NwPlayer player);

        void UpdateQuest(NwPlayer player, string questTag);
        void ClearQuest(NwPlayer player, string questTag);

        void UpdateProgress(NwPlayer player, ObjectiveWrapper objective);
        void ClearProgress(NwPlayer player, ObjectiveWrapper objective);

        void UpdateStageProgress(NwPlayer player, QuestStageWrapper stage);
        void ClearStageProgress(NwPlayer player, QuestStageWrapper stage);
    }
}