using System;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

using NLog;

using NWN.Core;

using MySQLClient;
using CharactersRegistry;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    [ServiceBinding(typeof(QuestsService))]
    internal sealed class QuestsService : IDisposable
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly MySQLService _mySQL;
        private readonly CharactersRegistryService _charactersRegistry;

        private readonly QuestPackManager _questPackMan;
        private readonly QuestManager _questMan;

        public QuestsService(MySQLService mySQL, CharactersRegistryService charactersRegistry, PluginStorageService pluginStorage)
        {
            string questPacksPath = pluginStorage.GetPluginStoragePath(typeof(QuestsService).Assembly);

            _questPackMan = new(pluginStorage.GetPluginStoragePath(typeof(QuestsService).Assembly));

            _mySQL = mySQL;
            _charactersRegistry = charactersRegistry;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnClientLeave += OnClientLeave;

            _questMan = new();
        }

        void OnClientEnter(ModuleEvents.OnClientEnter data)
        {
            _ = LoadQuestsAsync(data.Player);
        }

        async Task LoadQuestsAsync(NwPlayer player)
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(200));

            if (!_charactersRegistry.KickPlayerIfCharacterNotRegistered(player, out var pc))
                return;
                
            if(IsOnQuest(player,"test_quest_1", out var stageId))
            {
                await ResumeNotCompletedQuestTask(player,"test_quest_1",stageId);
            }
            else if(HasCompletedQuest(player,"test_quest_1",out stageId))
            {
                _questMan.MarkQuestAsCompleted(player, "test_quest_1", stageId);
            }

            return;

            _mySQL.QueryBuilder.Select("", ",,,")
                .Where(ServerData.DataProviders.PlayerSQLMap.UUID, pc.UUID.ToUUIDString());

            List<Task<bool>> resumeNotCompletedQuestsTasks = new();
            List<Task<bool>> writeCompletedQuestsTasks = new();

            using (var result = _mySQL.ExecuteQuery())
            {
                if (!result.HasData) return;

                foreach (var row in result)
                {
                    if (row.TryGet<string>(0, out var questTag)
                        && row.TryGet<int>(1, out var stage)
                        && row.TryGet<bool>(2, out var isCompleted))
                    {
                        if (!isCompleted) resumeNotCompletedQuestsTasks.Add(ResumeNotCompletedQuestTask(player, questTag, stage));
                        else _questMan.MarkQuestAsCompleted(player, questTag, stage);
                    }
                }
            }

            await NwTask.WhenAll(resumeNotCompletedQuestsTasks);
            await NwTask.WhenAll(writeCompletedQuestsTasks);
        }

        async Task<bool> ResumeNotCompletedQuestTask(NwPlayer player, string questTag, int stageId)
        {
            var quest = _questMan.GetCachedQuest(questTag);
            bool shouldRegisterQuest = quest == null;
            if (quest == null)
            {
                if (!_questPackMan.TryGetQuestImmediate(questTag, out var q))
                    return false;
                quest = new(q);
            }

            var stage = quest.GetStage(stageId);
            bool shouldRegisterStage = stage == null;
            if (stage == null)
            {
                if (!_questPackMan.TryGetQuestStageImmediate(quest.Tag, stageId, out var s))
                    return false;
                stage = new(s);
            }

            if (shouldRegisterQuest && !_questMan.RegisterQuest(quest)) return false;
            if (shouldRegisterStage && !quest.RegisterStage(stage)) return false;

            await NwTask.SwitchToMainThread();

            if (!player.IsValid)
            {
                if (shouldRegisterStage) quest.UnregisterStage(stage);
                if (shouldRegisterQuest) _questMan.UnregisterQuest(quest);
                return false;
            }

            stage.TrackProgress(player);

            return true;
        }


        void OnClientLeave(ModuleEvents.OnClientLeave data) => _questMan.ClearPlayer(data.Player);


        [ScriptHandler("qs_at")]
        ScriptHandleResult QuestSystem_ActionTaken(CallInfo data)
        {
            var pc = NWScript.GetPCSpeaker().ToNwObjectSafe<NwCreature>();

            if (pc == null || !pc.IsValid)
            {
                _log.Error("No PC speaker");
                return ScriptHandleResult.NotHandled;
            }

            var player = pc.ControllingPlayer;
            if (player == null || !player.IsValid)
            {
                _log.Error("No controlling player");
                return ScriptHandleResult.NotHandled;
            }

            bool validParams = true;
            if (TryParseParameters(data.ScriptParams["CompleteQuest"], out var parsedCompleteQuestParams)
            & TryParseParameters(data.ScriptParams["ClearQuest"], out var parsedClearQuestParams)
            & TryParseParameters(data.ScriptParams["GiveQuest"], out var parsedGiveQuestParams))
            {
                if (parsedCompleteQuestParams != null && parsedCompleteQuestParams.Values.Any(arr => arr.Length == 0))
                {
                    _log.Error("\'CompleteQuest\' action must take at least one stage ID as parameter.");
                    validParams = false;
                }

                if (parsedClearQuestParams != null && parsedClearQuestParams.Values.Any(arr => arr.Length > 0))
                {

                    _log.Error("\'ClearQuest\' action can't take stage ID as parameter.");
                    validParams = false;
                }

                if (parsedGiveQuestParams != null && parsedGiveQuestParams.Values.Any(arr => arr.Length != 1))
                {
                    _log.Error("\'GiveQuest\' action must take exactly one stage ID as parameter.");
                    validParams = false;
                }

            }

            if (!validParams)
            {
                _log.Warn("Failed to parse script parameters for conversation action. See \'qs_at.nss\' for formatting instructions.");
                return ScriptHandleResult.NotHandled;
            }

            if (parsedCompleteQuestParams != null)
            {
                foreach (var kvp in parsedCompleteQuestParams)
                {
                    var quest = _questMan.GetCachedQuest(kvp.Key);
                    if (quest == null) continue;
                    foreach (var id in kvp.Value)
                    {
                        var stage = quest.GetStage(id);
                        if (stage != null && stage.IsTracking(player))
                        {
                            _ = CompleteQuestOnStage(player, kvp.Key, id);
                            break;
                        }
                    }
                }
            }

            if (parsedClearQuestParams != null)
            {
                foreach (var key in parsedClearQuestParams.Keys)
                    _ = ClearQuest(player, key);
            }

            if (parsedGiveQuestParams != null)
            {
                foreach (var kvp in parsedGiveQuestParams)
                    _ = SetQuestStage(player, kvp.Key, kvp.Value[0]);
            }

            return ScriptHandleResult.Handled;
        }

        [ScriptHandler("qs_taw")]
        ScriptHandleResult QuestSystem_TextAppearWhen(CallInfo data)
        {
            var pc = NWScript.GetPCSpeaker().ToNwObjectSafe<NwCreature>();

            if (pc == null || !pc.IsValid)
            {
                _log.Error("No PC speaker");
                return ScriptHandleResult.NotHandled;
            }

            var player = pc.ControllingPlayer;
            if (player == null || !player.IsValid)
            {
                _log.Error("No controlling player");
                return ScriptHandleResult.NotHandled;
            }

            bool validParams =
                TryParseParameters(data.ScriptParams["IsOnQuest"], out var parsedIsOnQuestParams) &
                TryParseParameters(data.ScriptParams["IsNotOnQuest"], out var parsedIsNotOnQuestParams) &
                TryParseParameters(data.ScriptParams["CompletedQuest"], out var parsedCompletedQuestParams) &
                TryParseParameters(data.ScriptParams["NotCompletedQuest"], out var parsedNotCompletedQuestParams);

            if (!validParams)
            {
                _log.Warn("Failed to parse script parameters for conversation action. See \'qs_at.nss\' for formatting instructions.");
                return ScriptHandleResult.NotHandled;
            }


            if (parsedIsOnQuestParams != null)
            {
                bool isOnQuest = false;

                foreach (var kvp in parsedIsOnQuestParams)
                {
                    if (IsOnQuest(player, kvp.Key, out var stageId)
                        && (kvp.Value.Length == 0 || kvp.Value.Contains(stageId)))
                    {
                        isOnQuest = true;
                        break;
                    }
                }

                if (!isOnQuest) return ScriptHandleResult.False;
            }

            if (parsedIsNotOnQuestParams != null)
            {
                foreach (var kvp in parsedIsNotOnQuestParams)
                {
                    if (IsOnQuest(player, kvp.Key, out var stageId)
                        && (kvp.Value.Length == 0 || kvp.Value.Contains(stageId)))
                        return ScriptHandleResult.False;
                }
            }

            if (parsedCompletedQuestParams != null)
            {
                bool completed = false;

                foreach (var kvp in parsedCompletedQuestParams)
                {
                    if (HasCompletedQuest(player, kvp.Key, out var stageId)
                        && (kvp.Value.Length == 0 || kvp.Value.Contains(stageId)))
                    {
                        completed = true;
                        break;
                    }
                }

                if (!completed) return ScriptHandleResult.False;
            }

            if (parsedNotCompletedQuestParams != null)
            {
                foreach (var kvp in parsedNotCompletedQuestParams)
                {
                    if (HasCompletedQuest(player, kvp.Key, out var stageId)
                        && (kvp.Value.Length == 0 || kvp.Value.Contains(stageId)))
                        return ScriptHandleResult.False;
                }
            }

            return ScriptHandleResult.True;
        }

        private static bool TryParseParameters(string? parameters, out Dictionary<string, int[]>? parsedParameters)
        {
            parsedParameters = null;

            if (string.IsNullOrEmpty(parameters)) return true;


            var dict = new Dictionary<string, int[]>();

            bool failed = false;

            var args = parameters.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var arg in args)
            {
                var split = arg.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (split.Length == 0 || split.Length > 2)
                {
                    _log.Warn($"Invalid format: \"{arg}\"");
                    failed = true;
                    continue;
                }

                string questTag = split[0];

                if (!string.IsNullOrWhiteSpace(questTag))
                {
                    _log.Warn($"Invalid quest tag \'{split[0]}\'");
                }
                else if (split.Length == 1 && !failed)
                {
                    dict.Add(questTag, Array.Empty<int>());
                }
                else if (split.Length == 2)
                {
                    var ids = split[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var arr = new int[ids.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (!int.TryParse(ids[i], out arr[i]))
                        {
                            _log.Warn($"Invalid stage ID: \'{ids[i]}\'");
                            failed = true;
                            break;
                        }
                    }

                    if (!failed) dict.Add(questTag, arr);
                }
            }

            failed = !(dict.Count > 0 && !failed);

            if (!failed) parsedParameters = dict;

            return !failed;
        }


        private bool SetQuestStage(NwPlayer player, string questTag, int stageId)
        {
            QuestWrapper? quest = _questMan.GetCachedQuest(questTag);

            bool shouldRegisterQuest = quest == null;
            if (quest == null)
            {
                if (!_questPackMan.TryGetQuestImmediate(questTag, out var q))
                    return false;

                quest = new(q);
            }

            var stage = quest.GetStage(stageId);
            bool shouldRegisterStage = stage == null;
            if (stage == null)
            {
                if (!_questPackMan.TryGetQuestStageImmediate(quest.Tag, stageId, out var s))
                    return false;

                stage = new(s);
            }


            if (shouldRegisterQuest && !_questMan.RegisterQuest(quest))
                return false;

            if (shouldRegisterStage && !quest.RegisterStage(stage))
                return false;

            stage.TrackProgress(player);
            stage.ScheduleJournalUpdate(player);
            // todo: schedule lazy database update

            return true;
        }

        /// <summary>
        /// Reset data for the player, so they can start the quest again.
        /// </summary>
        /// <returns>False if the player is not on this quest</returns>
        public bool ClearQuest(NwPlayer player, string questTag)
        {
            if (IsOnQuest(player, questTag, out var stageId))
            {
                var stage = (_questMan.GetCachedQuest(questTag)?.GetStage(stageId)) ?? throw new InvalidOperationException("Failed to get stage by id returned from IsOnQuest");
                stage.StopTracking(player);
                if (!stage.IsActive)
                {
                    var quest = stage.Quest ?? throw new InvalidOperationException("Stage returned it from GetStage(id) has no parent");
                    if (!quest.UnregisterStage(stage)) throw new InvalidOperationException("Failed to unregister stage");
                    if (!_questMan.UnregisterQuest(quest)) throw new InvalidOperationException("Failed to unregister quest");
                }
            }
            return false;
        }

        private bool CompleteQuestOnStage(NwPlayer player, string questTag, int stageId)
        {
            //if(!IsOnQuestStage(player,questTag,stageId)) return false;

            var quest = _questMan.GetCachedQuest(questTag);
            var stage = quest?.GetStage(stageId);

            if (
                quest == null
                || stage == null
                || stage.IsTracking(player)
                || !player.IsValid
                || player.ControlledCreature == null
                || !player.ControlledCreature.IsValid
                )
                return false;


            //stage.Reward.GrantReward(player.ControlledCreature!);

            // grant reward

            // save to database

            return true;
        }


        // public async ValueTask GrantReward(NwCreature creature)
        // {
        //     creature.Xp += Math.Max(0, Xp);

        //     creature.GiveGold(Math.Max(0, Gold));

        //     creature.GoodEvilValue = Math.Clamp(creature.GoodEvilValue + GoodEvilChange, 0, 100);
        //     creature.LawChaosValue = Math.Clamp(creature.LawChaosValue + LawChaosChange, 0, 100);

        //     if (Items.Count == 0) return;

        //     foreach (var kvp in Items)
        //     {
        //         _ = await NwItem.Create(kvp.Key, creature);
        //     }
        // }







        /// <summary>
        /// Check if player is currently on any stage of specified quest (not including completed quests).
        /// </summary>
        /// <param name="stageId">Stage of the quest the player is currently on, -1 if not on the quest.</param>
        public bool IsOnQuest(NwPlayer player, string questTag, out int stageId)
        {
            stageId = -1;
            var quest = _questMan.GetCachedQuest(questTag);
            if (quest == null) return false;
            var qs = quest.Stages.FirstOrDefault(s => s.IsTracking(player));
            if (qs != null)
            {
                stageId = qs.ID;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if player is currently on the exact stage of specified quest (not including completed quests).
        /// </summary>
        public bool IsOnQuestStage(NwPlayer player, string questTag, int stageId)
        {
            var quest = _questMan.GetCachedQuest(questTag);
            if (quest == null) return false;
            var stage = quest.GetStage(stageId);
            return stage != null && stage.IsTracking(player);
        }

        /// <summary>Check if the player has completed a quest with specified tag, and output the stage ID if so.</summary>
        /// <param name="stageId">Stage on which the quest was completed, or -1 if quest was not completed by the player</param>
        /// <returns>True if the player has completed the quest</returns>
        public bool HasCompletedQuest(NwPlayer player, string questTag, out int stageId) => _questMan.HasCompletedQuest(player, questTag, out stageId);

        public void Dispose()
        {
            _questPackMan.Dispose();
        }
    }
}