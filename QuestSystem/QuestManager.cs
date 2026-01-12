using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Anvil.API;
using MySQLClient;
using NLog;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    internal sealed class QuestManager : IDisposable, IQuestInterface, IQuestDatabase
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly QuestPackManager _questPackMan;
        private readonly MySQLService _mySQL;
        
        private readonly Dictionary<string, QuestWrapper> _loadedQuests = new();
        private readonly Dictionary<NwPlayer, Dictionary<string, int>> _completedQuests = new();
        private readonly Dictionary<QuestWrapper, Dictionary<NwPlayer, PlayerQuestData>> _playerData = new();

        
        private PlayerQuestData? this[string questTag, NwPlayer player] =>
            _loadedQuests.TryGetValue(questTag, out var wrapper) ?
            this[wrapper,player] : null;

        private PlayerQuestData? this[QuestWrapper wrapper, NwPlayer player] =>
            _playerData.TryGetValue(wrapper, out var playersData) &&
            playersData.TryGetValue(player, out var playerData) ?
            playerData : null;


        public QuestManager(string questPackDirectory, MySQLService mySQL)
        {
            _questPackMan = new(questPackDirectory);
            _mySQL = mySQL;
        }

        private void RegisterQuest(QuestWrapper wrapper)
        {
            _loadedQuests.Add(wrapper.Tag, wrapper);
            _playerData.Add(wrapper, new()); // make new container for playerdata
            wrapper.Completed += OnQuestCompleted;
            wrapper.Updated += OnQuestUpdated;
            wrapper.Advanced += OnQuestAdvanced;
        }

        private void UnregisterQuest(QuestWrapper wrapper)
        {                   
            wrapper.Completed -= OnQuestCompleted;
            wrapper.Updated -= OnQuestUpdated;

            wrapper.Dispose();

            _playerData.Remove(wrapper); // destroy created container for playerdata
            _loadedQuests.Remove(wrapper.Tag);
        }

        private void OnQuestCompleted(QuestWrapper quest, NwPlayer player)
        {
            _log.Warn(" - Quest Completed Handler");
            CompleteQuest(player, quest.Tag);
        }

        private void OnQuestUpdated(QuestWrapper quest, NwPlayer player)
        {
            _log.Warn(" - Quest Updated Handler");
            var pd = this[quest,player];
            if(pd == null) return;
            pd.Update();
        }

        private void OnQuestAdvanced(QuestWrapper quest, NwPlayer player, int nextStageID)
        {
            _log.Warn(" - Quest Advanced Handler");
            GiveQuest(player, quest.Tag, nextStageID);
        }

        /// <remarks>This method will try to read the quest and/or stage from a file, and cache missing resources if they're not present in the dictionary</remarks>
        private bool TryGetOrLoadQuestStage(string questTag, int stageId, [NotNullWhen(true)] out QuestWrapper? questWrapper, [NotNullWhen(true)] out QuestStageWrapper? stageWrapper)
        {
            bool questLoaded = false;
            
            // load and store the quest in RAM, if not cached already
            if (!_loadedQuests.TryGetValue(questTag, out questWrapper))
            {
                if (!_questPackMan.TryGetQuestImmediate(questTag, out var quest))
                {
                    _log.Error($"Failed to load quest \'{questTag}\' from the pack");
                    stageWrapper = null;
                    return false;
                }
                questWrapper = new(quest);

                RegisterQuest(questWrapper);

                questLoaded = true;
            }

            stageWrapper = questWrapper[stageId]; // will return null, if the quest was loaded from file in this operation, or stage is not cached

            if(stageWrapper != null) return true; // early return if both quest and stage were already in the cache

            // load the stage from file, if its not present in the cache
            if(!_questPackMan.TryGetQuestStageImmediate(questTag, stageId, out var stage))
            {
                _log.Error($"Failed to load stage {stageId} of quest \'{questTag}\' from the pack");

                if(questLoaded) // unload the quest on failure, if it was cached by this operation
                {
                    UnregisterQuest(questWrapper);
                }
                return false;
            }

            // store the stage in RAM
            stageWrapper = new(stage);

            if (!questWrapper.RegisterStage(stageWrapper))
            {
                if(questLoaded)
                {
                    _playerData.Remove(questWrapper);
                    _loadedQuests.Remove(questTag);
                }

                _log.Error($"Stage {stageId} is already registered under quest \'{questTag}\'");

                questWrapper = null;
                stageWrapper = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes any quest-related data for this player from memory
        /// </summary>
        /// <param name="player"></param>
        public void ClearPlayer(NwPlayer player)
        {
            _ = _completedQuests.Remove(player);

            List<QuestWrapper> touchedQuests = new();

            foreach(var kvp in _playerData)
            {   
                if(!kvp.Value.TryGetValue(player, out var pd))
                    continue;

                var currentStage = pd.CurrentStage;
                if(currentStage != null)
                {
                    currentStage.StopTracking(player);
                    CleanupStageIfNotTrackingPlayers(kvp.Key,currentStage);
                }
                touchedQuests.Add(kvp.Key);
            }

            foreach(var quest in touchedQuests)
                CleanupQuestIfNoStages(quest);
            
        }

        /// <inheritdoc cref="IQuestInterface.GiveQuest"/>
        /// <inheritdoc cref="TryGetOrLoadQuestStage"/>
        public bool GiveQuest(NwPlayer player, string questTag, int stageId)
        {
            _log.Info(" - - - Giving quest");
            if(!player.IsValid) return false;

            if(!TryGetOrLoadQuestStage(questTag, stageId, out var quest, out var stage))
                return false;

            var existingData = this[quest,player];

            if(existingData == null) // cache player's data and start to track the progress
            {
                _log.Info("> Creating new playerdata <");
                var newData = new PlayerQuestData(player, quest);

                _playerData[quest].Add(player, newData);

                newData.PushStage(stage);
                newData.CurrentStage?.TrackProgress(player);
            }
            else if(existingData.CurrentStage == stage){
                _log.Info("> Resetting progress <");
                existingData.ResetCurrentStageProgress();
            }
            else
            {
                _log.Info("> Stage transition <");

                var oldStage = existingData.CurrentStage;
                oldStage?.StopTracking(player);
                existingData.PushStage(stage);
                existingData.CurrentStage?.TrackProgress(player);

                CleanupStageIfNotTrackingPlayers(quest,oldStage);
                CleanupQuestIfNoStages(quest);
            }

            return true;
        }

        private static void CleanupStageIfNotTrackingPlayers(QuestWrapper quest, QuestStageWrapper? stage)
        {
            if(stage == null || stage.TrackedPlayersCount > 0) return;

            quest.UnregisterStage(stage);
        }

        private void CleanupQuestIfNoStages(QuestWrapper quest)
        {
            if(quest.RegisteredStages <= 0)
                UnregisterQuest(quest);
        }

        public bool ClearQuest(NwPlayer player, string questTag)
        {
            _log.Info(" - - - Clearing quest");

            ((IQuestDatabase)this).ClearQuest(player, questTag);

            if(_completedQuests.TryGetValue(player, out var dict)) 
                dict.Remove(questTag);
            
            else if(_loadedQuests.TryGetValue(questTag, out var quest))
            {
                var pd = this[quest, player];

                if(pd == null) return false;

                var stage = pd.CurrentStage;

                pd.Dispose();

                // wipe journal
                if (player.IsValid)
                {
                    var pc = player.ControlledCreature;
                    if(pc != null && pc.IsValid)
                        NWN.Core.NWScript.RemoveJournalQuestEntry(questTag, pc.ObjectId, 0, 0);
                }

                _playerData[quest].Remove(player);

                stage?.StopTracking(player);

                CleanupStageIfNotTrackingPlayers(quest, stage);
                CleanupQuestIfNoStages(quest);
            }
            else return false;
            
            return true;
        }

        public bool CompleteQuest(NwPlayer player, string questTag, int stageId = -1)
        {
            _log.Info(" - - - Completing quest");
            if(HasCompletedQuest(player, questTag, out var completedStageId))
            {
                if(stageId == completedStageId)
                    return true; // early return if nothing to change

                else if(stageId < 0)
                    return false; // meet interface requirements

                else
                {
                    _log.Warn($"Overriding completed quest \'{questTag}\' stage {completedStageId} -> {stageId}");
                }
            }

            if(!_completedQuests.TryGetValue(player, out var dict))
            {
                dict = new()
                {
                    { questTag, stageId }
                };
                _completedQuests.Add(player, dict);
            }
            else dict.Add(questTag,stageId);


            ((IQuestDatabase)this).UpdateQuest(player, questTag);


            if(!IsOnQuest(player, questTag, out var _))
                return true;


            var quest = _loadedQuests[questTag];
            var pd = this[quest,player]!;
            var stage = pd.CurrentStage;

            pd.IsStageCompleted = true;
            pd.IsQuestCompleted = true;

            pd.Dispose();
            _playerData[quest].Remove(player);
            
            stage?.StopTracking(player);

            CleanupStageIfNotTrackingPlayers(quest, stage);
            CleanupQuestIfNoStages(quest);

            return true;
        }



        /// <inheritdoc cref="IQuestInterface.GiveQuest"/>
        /// <inheritdoc cref="TryGetOrLoadQuestStage"/>
        public bool CompleteStage(NwPlayer player, string questTag, int stageId = -1)
        {
            _log.Info(" - - - Completing quest stage");
            if(!IsOnQuest(player, questTag, out var currentStageId))
            {
                if(stageId < 0 || !GiveQuest(player, questTag, stageId))
                    return false;

                return CompleteStage(player, questTag, stageId);
            }

            var stage = _loadedQuests[questTag][stageId < 0 ? currentStageId : stageId];

            if(stage == null) return false;

            if(!stage.ManualComplete(player)) return false;

            this[questTag,player]!.IsStageCompleted = true;
            return true;
        }

        /// <inheritdoc cref="IQuestInterface.IsOnQuest(NwPlayer, string, out int)"/>
        public bool IsOnQuest(NwPlayer player, string questTag, out int stageId)
        {
            stageId = this[questTag, player]?.CurrentStage?.ID ?? -1;
            return stageId >= 0;
        }
    
        /// <inheritdoc cref="IQuestInterface.HasCompletedQuest(NwPlayer, string, out int)"/>
        public bool HasCompletedQuest(NwPlayer player, string questTag, out int stageId)
        {
            stageId = -1;

            if(_completedQuests.TryGetValue(player, out var dict) && dict.TryGetValue(questTag, out stageId))
                return true;

            return false;
        }


        private bool isDisposed = false;
        public void Dispose()
        {
            _log.Warn("DISPOSING");
            ObjectDisposedException.ThrowIf(isDisposed, this);
            isDisposed = true;

            foreach(var kvp in _playerData)
            {
                foreach(var data in kvp.Value)
                {
                    data.Value.Dispose();
                }
            }

            foreach(var quest in _loadedQuests.Values)
                quest.Dispose();

            _questPackMan.Dispose();
        }

        void IQuestDatabase.LazyLoadPlayerQuests(NwPlayer player)
        {
            _log.Info(" -- Lazy loading player quests (fake)");
        }

        void IQuestDatabase.UpdateQuest(NwPlayer player, string questTag)
        {
            _log.Info(" -- Updating player quest in database (fake)");
        }

        void IQuestDatabase.ClearQuest(NwPlayer player, string questTag)
        {
            _log.Info(" -- Clearing player quest from database (fake)");
        }

        void IQuestDatabase.UpdateProgress(NwPlayer player, ObjectiveWrapper objective)
        {
            _log.Info(" -- Updating player quest objective progress in database (fake)");
        }

        void IQuestDatabase.ClearProgress(NwPlayer player, ObjectiveWrapper objective)
        {
            _log.Info(" -- Clearing player quest objective progress from database (fake)");
        }

        void IQuestDatabase.UpdateStageProgress(NwPlayer player, QuestStageWrapper stage)
        {
            _log.Info(" -- Updating player quest stage progress in database (fake)");
        }

        void IQuestDatabase.ClearStageProgress(NwPlayer player, QuestStageWrapper stage)
        {
            _log.Info(" -- Clearing player quest stage progress from database (fake)");
        }
    }
}