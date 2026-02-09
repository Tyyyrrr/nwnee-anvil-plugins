using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.Services;
using Anvil.API.Events;
using NWN.Core;
using NLog;

using MySQLClient;
using CharactersRegistry;

using QuestSystem.Wrappers.Objectives;

namespace QuestSystem
{
    [ServiceBinding(typeof(QuestsService))]
    public sealed class QuestsService : IDisposable
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly MySQLService _mySQL;
        private readonly CharactersRegistryService _charactersRegistry;

        private readonly QuestManager _questMan;
        public IQuestInterface Interface => _questMan;

        public QuestsService(MySQLService mySQL, CharactersRegistryService charactersRegistry, PluginStorageService pluginStorage, EventService events)
        {
            ObjectiveWrapper.EventService = events;

            _mySQL = mySQL;
            _charactersRegistry = charactersRegistry;

            NwModule.Instance.OnModuleLoad += OnModuleLoad;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnClientLeave += OnClientLeave;

            _questMan = new QuestManager(pluginStorage.GetPluginStoragePath(typeof(QuestsService).Assembly), _mySQL);
        }

        void OnModuleLoad(ModuleEvents.OnModuleLoad _)
        {
            foreach(var area in NwModule.Instance.Areas)
            {
                area.OnEnter += d =>
                {
                    var variable = d.EnteringObject.GetObjectVariable<LocalVariableInt>("InvisibleByDefault");

                    if(variable.HasValue && variable.Value == 1)
                    {
                        variable.Delete();
                        d.EnteringObject.VisibilityOverride = VisibilityMode.Hidden;
                    }
                };

                foreach(var obj in area.Objects)
                {                    
                    var variable = obj.GetObjectVariable<LocalVariableInt>("InvisibleByDefault");

                    if(variable.HasValue && variable.Value == 1)
                    {
                        variable.Delete();
                        obj.VisibilityOverride = VisibilityMode.Hidden;
                    }
                }
            }
        }

        public void Dispose()
        {
            ((IDisposable)Interface).Dispose();
        }

        void OnClientEnter(ModuleEvents.OnClientEnter data)
        {
            if(!_charactersRegistry.KickPlayerIfCharacterNotRegistered(data.Player, out var pc))
                return;

            ((IQuestDatabase)_questMan).LazyLoadPlayerQuests(data.Player);
        }

        void OnClientLeave(ModuleEvents.OnClientLeave data)
        {
            _questMan.ClearPlayer(data.Player);
        }

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
            & TryParseParameters(data.ScriptParams["CompleteStage"], out var parsedCompleteStageParams)
            & TryParseParameters(data.ScriptParams["ClearQuest"], out var parsedClearQuestParams)
            & TryParseParameters(data.ScriptParams["GiveQuest"], out var parsedGiveQuestParams))
            {
                if (parsedCompleteQuestParams != null && parsedCompleteQuestParams.Values.Any(arr => arr.Length == 0))
                {
                    _log.Error("\'CompleteQuest\' action must take at least one stage ID as parameter.");
                    validParams = false;
                }

                if(parsedCompleteStageParams != null && parsedCompleteStageParams.Values.Any(arr => arr.Length == 0))
                {
                    _log.Error("\'CompleteQuestStage\' action must take at least one stage ID as parameter");
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
                    if(_questMan.IsOnQuest(player, kvp.Key, out var stageId))
                    {
                        _ = _questMan.CompleteQuest(player,kvp.Key,kvp.Value[0]);
                    }
                }
            }

            if (parsedClearQuestParams != null)
            {
                foreach (var key in parsedClearQuestParams.Keys)
                    _questMan.ClearQuest(player, key);
            }


            if(parsedCompleteStageParams != null)
            {
                foreach(var kvp in parsedCompleteStageParams)
                {
                    if(_questMan.IsOnQuest(player, kvp.Key, out var stageId))
                    {
                        _ = _questMan.CompleteStage(player, kvp.Key, kvp.Value[0]);
                    }
                }
            }
            
            if (parsedGiveQuestParams != null)
            {
                foreach (var kvp in parsedGiveQuestParams)
                    _ = _questMan.GiveQuest(player, kvp.Key, kvp.Value[0]);
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
                    string str = kvp.Value.Length == 0 ?
                        $"Checking if player is on quest {kvp.Key}" :
                        $"Checking if player is on any stage from {string.Join(',',kvp.Value)} of quest {kvp.Key}";

                    _log.Info(str);

                    if (_questMan.IsOnQuest(player, kvp.Key, out var stageId)
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
                    string str = kvp.Value.Length == 0 ?
                        $"Checking if player is NOT on quest {kvp.Key}" :
                        $"Checking if player is NOT on any stage from {string.Join(',',kvp.Value)} of quest {kvp.Key}";

                    _log.Info(str);

                    if (_questMan.IsOnQuest(player, kvp.Key, out var stageId)
                        && (kvp.Value.Length == 0 || kvp.Value.Contains(stageId)))
                        return ScriptHandleResult.False;
                }
            }

            if (parsedCompletedQuestParams != null)
            {
                bool completed = false;

                foreach (var kvp in parsedCompletedQuestParams)
                {                    
                    string str = kvp.Value.Length == 0 ?
                        $"Checking if player has completed quest {kvp.Key}" :
                        $"Checking if player has completed quest {kvp.Key} on any stage from {string.Join(',',kvp.Value)}";

                    _log.Info(str);

                    if (_questMan.HasCompletedQuest(player, kvp.Key, out var stageId)
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
                    string str = kvp.Value.Length == 0 ?
                        $"Checking if player has NOT completed quest {kvp.Key}" :
                        $"Checking if player has NOT completed quest {kvp.Key} on any stage from {string.Join(',',kvp.Value)}";

                    _log.Info(str);

                    if (_questMan.HasCompletedQuest(player, kvp.Key, out var stageId)
                        && (kvp.Value.Length == 0 || kvp.Value.Contains(stageId)))
                        return ScriptHandleResult.False;
                }
            }

            return ScriptHandleResult.True;
        }
        private static bool TryParseParameters(string? parameters, out Dictionary<string, int[]>? parsedParameters)
        {
            parsedParameters = null;

            if (string.IsNullOrEmpty(parameters))
                return true;

            var dict = new Dictionary<string, int[]>();
            bool failed = false;

            var args = parameters.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var arg in args)
            {
                if (failed) break;

                var split = arg.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (split.Length == 0 || split.Length > 2)
                {
                    _log.Warn($"Invalid format: \"{arg}\"");
                    failed = true;
                    break;
                }

                string questTag = split[0];

                if (string.IsNullOrWhiteSpace(questTag))
                {
                    _log.Warn($"Invalid quest tag '{split[0]}'");
                    failed = true;
                    break;
                }

                if (split.Length == 1)
                {
                    if (!dict.TryAdd(questTag, Array.Empty<int>()))
                    {
                        _log.Warn($"Duplicate quest tag '{questTag}'");
                        failed = true;
                        break;
                    }

                    continue;
                }

                // split.Length == 2
                var ids = split[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var arr = new int[ids.Length];

                for (int i = 0; i < arr.Length; i++)
                {
                    if (!int.TryParse(ids[i], out arr[i]))
                    {
                        _log.Warn($"Invalid stage ID: '{ids[i]}'");
                        failed = true;
                        break;
                    }
                }

                if(failed) break;

                if (!dict.TryAdd(questTag, arr))
                {
                    _log.Warn($"Duplicate quest tag '{questTag}'");
                    failed = true;
                    break;
                }
            }

            if (failed || dict.Count == 0)
                return false;

            parsedParameters = dict;
            return true;
        }
    }
}