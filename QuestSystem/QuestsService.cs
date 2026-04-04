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
using ServerData;
using System.Diagnostics.CodeAnalysis;
using NLog.LayoutRenderers;

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

            // NwModule.Instance.OnModuleLoad += OnModuleLoad;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnClientLeave += OnClientLeave;

            _questMan = new QuestManager(pluginStorage.GetPluginStoragePath(typeof(QuestsService).Assembly), _mySQL);

            _mySQL.ExecuteQuery(DataProviders.QuestSQLMap.CreateTableIfNotExistsQuery);
            _mySQL.ExecuteQuery(DataProviders.QuestObjectiveSQLMap.CreateTableIfNotExistsQuery);
            _mySQL.ExecuteQuery(DataProviders.QuestVisibilitySQLMap.CreateTableIfNotExistsQuery);
        }

        // void OnModuleLoad(ModuleEvents.OnModuleLoad _)
        // {
        //     foreach(var area in NwModule.Instance.Areas)
        //     {
        //         area.OnEnter += d =>
        //         {
        //             if(d.EnteringObject is NwCreature creature && creature.IsValid && creature.IsPlayerControlled(out var player) && player.IsValid)
        //             {
        //                 // run visibility override update of every object for this player in this area
        //                 _questMan.RefreshVisibilityOverridesForPlayerEnteringArea(player, area);
        //             }
        //             else
        //             {
        //                 // run visibility override update of this object for every player in this area
        //                 _questMan.RefreshVisibilityOverridesForObjectEnteringArea(d.EnteringObject, area);
        //             }
        //         };

        //         foreach(var obj in area.Objects)
        //         {                    
        //             var variable = obj.GetObjectVariable<LocalVariableInt>("InvisibleByDefault");

        //             if(variable.HasValue && variable.Value == 1)
        //             {
        //                 variable.Delete();
        //                 obj.VisibilityOverride = VisibilityMode.Hidden;
        //             }
        //         }
        //     }
        // }



        static bool GetItemParamsData(string scriptParams, [NotNullWhen(true)] out Dictionary<string, int>? items, bool both = false)
        {
            if (string.IsNullOrEmpty(scriptParams))
            {
                items = null;
                return false;
            }

            var splitA = scriptParams.Split(';');

            items = new Dictionary<string, int>();

            foreach(var str in splitA)
            {
                var splitB = str.Split(':');

                if (both)
                {
                    if(splitB.Length < 2) return false;

                    var item = splitB[0]+':'+splitB[1];

                    if(splitB.Length > 2 && int.TryParse(splitB[2].Trim(), out var amount))
                        items.TryAdd(item, amount);

                    else items.TryAdd(item, 1);
                }
                else
                {                
                    var item = splitB[0];

                    if(splitB.Length > 1 && int.TryParse(splitB[1].Trim(), out var amount))
                        items.TryAdd(item, amount);

                    else items.TryAdd(item, 1);
                }

            }

            return true;
        }

        static bool TakeItemsByTag(NwCreature pc, string scriptParams)
        {
            if(!GetItemParamsData(scriptParams, out var dict) || !HasItemsByTag(pc,dict))
                return false;

            TakeItemsByTag(pc, dict);
            return true;
        }
        static void TakeItemsByTag(NwCreature pc, Dictionary<string, int> items)
        {
            List<NwItem> itemsToDestroy = new();

            foreach(var item in pc.Inventory.Items)
            {
                if(items.TryGetValue(item.Tag, out var amount) && amount > 0)
                {
                    if(item.StackSize <= amount)
                    {
                        itemsToDestroy.Add(item);
                        items[item.Tag] -= item.StackSize;
                    }
                    else
                    {
                        item.StackSize -= amount;
                        items[item.Tag] = 0;
                    }
                }
            }

            foreach(var item in itemsToDestroy)
            {
                item.IsDestroyable = true;
                item.Destroy();
            }
        }

        static bool TakeItemsByResRef(NwCreature pc, string scriptParams)
        {
            if(!GetItemParamsData(scriptParams, out var dict) || !HasItemsByResRef(pc,dict))
                return false;

            TakeItemsByResRef(pc, dict);
            return true;
        }

        static void TakeItemsByResRef(NwCreature pc, Dictionary<string, int> items)
        {
            List<NwItem> itemsToDestroy = new();

            foreach(var item in pc.Inventory.Items)
            {
                if(items.TryGetValue(item.Tag, out var amount) && amount > 0)
                {
                    if(item.StackSize <= amount)
                    {
                        itemsToDestroy.Add(item);
                        items[item.ResRef] -= item.StackSize;
                    }
                    else
                    {
                        item.StackSize -= amount;
                        items[item.ResRef] = 0;
                    }
                }
            }

            foreach(var item in itemsToDestroy)
            {
                item.IsDestroyable = true;
                item.Destroy();
            }
        }

        static bool TakeItemsByTagAndResRef(NwCreature pc, string scriptParams)
        {
            if(!GetItemParamsData(scriptParams, out var dict, true) || !HasItemsByTagAndResRef(pc,dict))
                return false;

            TakeItemsByTagAndResRef(pc, dict);
            return true;
        }

        static void TakeItemsByTagAndResRef(NwCreature pc, Dictionary<string, int> items)
        {
            List<NwItem> itemsToDestroy = new();

            foreach(var item in pc.Inventory.Items)
            {
                var key = $"{item.Tag}:{item.ResRef}";

                if(items.TryGetValue(key, out var amount) && amount > 0)
                {
                    if(item.StackSize <= amount)
                    {
                        itemsToDestroy.Add(item);
                        items[key] -= item.StackSize;
                    }
                    else
                    {
                        item.StackSize -= amount;
                        items[key] = 0;
                    }
                }
            }

            foreach(var item in itemsToDestroy)
            {
                item.IsDestroyable = true;
                item.Destroy();
            }
        }


        static bool HasItemsByTag(NwCreature pc, string scriptParams)
        {
            if(!GetItemParamsData(scriptParams, out var dict))
                return false;

            return HasItemsByTag(pc, dict);
        }

        static bool HasItemsByTag(NwCreature pc, Dictionary<string, int> dict)
        {
            var clone = new Dictionary<string,int>(dict);

            foreach(var item in pc.Inventory.Items)
            {
                if(!dict.TryGetValue(item.Tag, out var amount) || amount < 0) continue;

                clone[item.Tag] -= item.StackSize;
            }

            return !clone.Any(kvp=>kvp.Value > 0);
        }

        static bool HasItemsByResRef(NwCreature pc, string scriptParams)
        {            
            if(!GetItemParamsData(scriptParams, out var dict))
                return false;

            return HasItemsByResRef(pc, dict);
        }
        static bool HasItemsByResRef(NwCreature pc, Dictionary<string, int> dict)
        {
            var clone = new Dictionary<string,int>(dict);

            foreach(var item in pc.Inventory.Items)
            {
                if(!dict.TryGetValue(item.ResRef, out var amount) || amount < 0) continue;

                clone[item.ResRef] -= item.StackSize;
            }

            return !clone.Any(kvp=>kvp.Value > 0);
        }
        static bool HasItemsByTagAndResRef(NwCreature pc, string scriptParams)
        {
            if(!GetItemParamsData(scriptParams, out var dict, true))
                return false;

            return HasItemsByTagAndResRef(pc,dict);
        }
        static bool HasItemsByTagAndResRef(NwCreature pc, Dictionary<string, int> dict)
        {
            var clone = new Dictionary<string,int>(dict);

            foreach(var item in pc.Inventory.Items)
            {
                string key = $"{item.Tag}:{item.ResRef}";

                if(!dict.TryGetValue(key, out var amount) || amount < 0) continue;

                clone[key] -= item.StackSize;
            }

            return !clone.Any(kvp=>kvp.Value > 0);
        }



        public void Dispose()
        {
            ((IDisposable)Interface).Dispose();
        }

        void OnClientEnter(ModuleEvents.OnClientEnter data)
        {
            if(!_charactersRegistry.KickPlayerIfCharacterNotRegistered(data.Player, out var pc))
                return;

            if(data.Player.IsDM) return;

            _questMan.LoadVisibilityOverrides(data.Player);

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

            if (player.IsDM)
            {
                return ScriptHandleResult.Handled;
            }

            string takeItemTagParams = data.ScriptParams["TakeItemTag"];
            string takeItemResRefParams = data.ScriptParams["TakeItemResRef"];
            string takeItemTagResRefParams = data.ScriptParams["TakeItemTagResRef"];

            if(!string.IsNullOrEmpty(takeItemTagParams))
                if(!TakeItemsByTag(pc,takeItemTagParams))
                {
                    _log.Warn("Failed to take item(s) by tag from PC inventory");
                    return ScriptHandleResult.Handled;
                }
            else if(!string.IsNullOrEmpty(takeItemResRefParams))
                if (!TakeItemsByResRef(pc,takeItemResRefParams))
                {
                    _log.Warn("Failed to take item(s) by resRef from PC inventory");
                    return ScriptHandleResult.Handled;
                }
            else if(!string.IsNullOrEmpty(takeItemTagResRefParams))
                if(!TakeItemsByTagAndResRef(pc,takeItemTagResRefParams))
                {
                    _log.Warn("Failed to take item(s) by tag and resRef from PC inventory");
                    return ScriptHandleResult.Handled;
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

            if (player.IsDM)
            {
                return ScriptHandleResult.False;
            }

            string hasItemTagParams = data.ScriptParams["HasItemTag"];
            string hasItemResRefParams = data.ScriptParams["HasItemResRef"];
            string hasItemTagResRefParams = data.ScriptParams["HasItemTagResRef"];

            bool result = string.IsNullOrEmpty(hasItemTagParams) || HasItemsByTag(pc,hasItemTagParams);
            result &= string.IsNullOrEmpty(hasItemResRefParams) || HasItemsByResRef(pc,hasItemResRefParams);
            result &= string.IsNullOrEmpty(hasItemTagResRefParams) || HasItemsByTagAndResRef(pc,hasItemTagResRefParams);

            if(!result) return ScriptHandleResult.False;

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

                    // _log.Info(str);

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

                    // _log.Info(str);

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

                    // _log.Info(str);

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

                    // _log.Info(str);

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