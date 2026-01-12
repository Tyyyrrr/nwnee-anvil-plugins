using Anvil.API;

namespace QuestSystem
{
    public interface IQuestInterface
    {
        /// <summary>
        /// Sets the player on the selected quest and stage, and updates the journal.<br/>
        /// This will NOT complete the previous stage, give reward or update old stage's journal text.<br/>
        /// If the player is already on this stage, the progress will be reset.
        /// </summary>
        /// <returns>False if the player is already on this quest, or initial stage does not exist.</returns>
        public bool GiveQuest(NwPlayer player, string questTag, int stageId);

        /// <summary>
        /// Mark the quest as 'completed' by the player on the selected stage.<br/>
        /// This will NOT complete the current (or selected) stage, give reward or update stage's journal text.
        /// </summary>
        /// <param name="stageId">If negative - quest will be completed on the CURRENT stage</param>
        /// <returns>False, if <paramref name="stageId"/> is negative, and the player is currently not on this quest.</returns>
        public bool CompleteQuest(NwPlayer player, string questTag, int stageId = -1);

        /// <summary>
        /// Manually complete the current (or selected) stage, give reward and update journal text.<br/>
        /// This will NOT automatically proceed to the next stage. Also, any objective rewards will be skipped.<br/><br/>
        /// If <paramref name="stageId"/> is non-negative, and the player is not on selected stage, they will be set to the selected stage without completing the previous stage (if any).
        /// </summary>
        /// <param name="stageId">If negative - complete the CURRENT stage</param>
        /// <returns>False, if <paramref name="stageId"/> is negative, and the player is currently not on this quest.</returns>
        public bool CompleteStage(NwPlayer player, string questTag, int stageId = -1);

        /// <summary>
        /// Removes all persistent data associated with the quest for the player, including journal entries and progress stored in database.
        /// </summary>
        /// <returns>False, if there was no data to wipe.</returns>
        public bool ClearQuest(NwPlayer player, string questTag);

        /// <param name="stageId">Current stage the player is on, or -1 if not on quest.</param>
        public bool IsOnQuest(NwPlayer player, string questTag, out int stageId);

        /// <param name="stageId">ID of the stage on which the player has completed the quest. -1 if not completed.</param>
        public bool HasCompletedQuest(NwPlayer player, string questTag, out int stageId);
    }
}