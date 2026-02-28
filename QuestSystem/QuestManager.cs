using System;
using System.Collections.Generic;

using Anvil.API;
using NLog;

using MySQLClient;

using QuestSystem.Graph;
using QuestSystem.Wrappers.Nodes;
using QuestSystem.Wrappers.Objectives;
using ServerData;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using ServerData.SQLSchema;
using System.Threading.Tasks;
using QuestSystem.Nodes;
using System.Text.RegularExpressions;

namespace QuestSystem
{
    internal sealed class QuestManager : IDisposable, IQuestInterface, IQuestDatabase
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();


        private readonly MySQLService _mySQL;
        private static readonly IQuestSQLMap _questSQLMap = DataProviders.QuestSQLMap;
        private static readonly IQuestObjectiveSQLMap _objectiveSQLMap = DataProviders.QuestObjectiveSQLMap;
        private static readonly IPlayerSQLMap _playerSQLMap = DataProviders.PlayerSQLMap;
        private static readonly IQuestVisibilitySQLMap _visibilitySQLMap = DataProviders.QuestVisibilitySQLMap;


        private readonly QuestPackManager _questPackMan;
        private readonly INodeLoader _nodeLoader;


        private readonly Dictionary<string, QuestGraph>  _loadedQuests = new();
        private readonly Dictionary<NwPlayer, Dictionary<string, int>> _completedQuests = new();


        private static QuestManager? _instance = null;
        public static QuestManager Instance => _instance ?? throw new InvalidOperationException("No singleton instance");


        private static readonly Dictionary<string, Dictionary<NwPlayer, PlayerQuestData>>  _questData = new();
        public static PlayerQuestData? GetPlayerQuestData(NwPlayer player, Quest? quest)
        {
            if(quest == null) return null;
            return _questData.TryGetValue(quest.Tag, out var dict) && dict.TryGetValue(player, out var data) ? data : null;
        }


        public QuestManager(string questPackDirectory, MySQLService mySQL)
        {
            if(_instance == null)
                _instance = this;
            else throw new InvalidOperationException("QuestManager singleton can be initialized only once.");

            PlayerQuestData.DataReady += OnPlayerDataReadyForUpdate;

            QuestGraph.QuestCompleted += OnQuestCompleted;
            _questPackMan = new(questPackDirectory);
            _nodeLoader = new NodeLoader();
            _mySQL = mySQL;

            foreach(var dict in _questData.Values)
                foreach(var p in dict.Values)
                    p.Dispose();
            
            _questData.Clear();
        }

        private void RegisterQuest(Quest quest)
        {
            _log.Info($"Registering quest {quest}");
            var graph = new QuestGraph(quest, _nodeLoader);
            _loadedQuests.Add(quest.Tag, graph);
            _questData.Add(quest.Tag, new());
        }

        private void UnregisterQuest(string tag)
        {
            var graph = _loadedQuests[tag];

            _log.Info($"Unregistering quest {graph.Quest}");
            var data = _questData[tag];

            _questData.Remove(tag);
            data.Values.DisposeAll();

            _loadedQuests.Remove(tag);
            graph.Dispose();
        }

        /// <summary>
        /// Removes any quest-related data for this player from memory
        /// </summary>
        public void ClearPlayer(NwPlayer player)
        {
            List<Quest> emptyGraphs = new();
            foreach(var graph in _loadedQuests.Values)
            {
                graph.RemovePlayer(player);
                if(graph.IsEmpty)
                    emptyGraphs.Add(graph.Quest);
            }
            foreach(var q in emptyGraphs)
                UnregisterQuest(q.Tag);
        }

        void OnQuestCompleted(string tag, NwPlayer player)
        {
            var graph = _loadedQuests[tag];

            if(!TryCaptureSnapshotWithErrorLog(graph, player, out var snapshot))
                return;

            int stageId = snapshot[snapshot[0]];

            if(!_completedQuests.ContainsKey(player))
                _completedQuests.Add(player, new());

            if(_completedQuests[player].TryGetValue(tag, out var id))
            {
                _log.Warn($"Overriding completed quest! Old stage: {id}, new stage: {stageId}");
                _completedQuests[player][tag] = stageId;
            }
            else _completedQuests[player].Add(tag,stageId);

            UpdateCompletedQuest(player,tag,snapshot);

            graph.RemovePlayer(player);

            if(graph.IsEmpty)
                UnregisterQuest(tag);

        }


        /// <inheritdoc cref="IQuestInterface.GiveQuest"/>
        public bool GiveQuest(NwPlayer player, string questTag, int stageId)
        {
            // _log.Info("Setting player on quest " + questTag +":"+stageId);
            if(!_loadedQuests.TryGetValue(questTag, out var graph))
            {
                if(!_questPackMan.TryGetQuestImmediate(questTag, out var quest))
                {
                    _log.Error("Failed to set player on quest. The quest was not found in any packs");
                    return false;
                }

                RegisterQuest(quest);

                graph = _loadedQuests[questTag];
            }

            if(!_questData[questTag].ContainsKey(player))
                _questData[questTag].Add(player, new(player, graph));

            if(graph?.AddPlayer(player, stageId) ?? false)
            {
                // _log.Info("Player set on quest!");
            }
            else
            {
                // _log.Info("Failed to set player on quest");
                return false;
            }

////////////////////////////
            ((IQuestDatabase)this).UpdateQuest(player, questTag); 
//                                                                      although it will be refreshed after some delay
//                                                                      we need it here too, in order to write to db immediately and avoid situations
//                                                                      where player logs out after completing stage, and gets reward multiple times by such exploit
//                                                                        (TODO: find a way to avoid repeated update for the specific player)
////////////////////////////

            return true;
        }

        bool LoadFromSnapshot(NwPlayer player, string questTag, int[] snapshot)
        {
            // _log.Info("Loading player quest "+questTag+"from snapshot");
            if(!_loadedQuests.TryGetValue(questTag, out var graph))
            {
                if(!_questPackMan.TryGetQuestImmediate(questTag, out var quest))
                {
                    _log.Error("Failed to load player quest form snapshot. The quest was not found in any packs");
                    return false;
                }

                RegisterQuest(quest);

                graph = _loadedQuests[questTag];
            }

            if(!_questData[questTag].ContainsKey(player))
                _questData[questTag].Add(player, new(player, graph));

            if(graph?.AddPlayer(player,snapshot) ?? false)
            {
                // _log.Info("Player loaded quest from snapshot!");

                if(!TryGetUUIDStringWithErrorLog(player, out var uuid) ||
                    !TryGetStageNodeWithErrorLog(graph,player, out var stageNode) )
                {
                    _log.Error("Failed to get current node or player uuid");
                    graph!.RemovePlayer(player);
                    return false;
                }



                _mySQL.QueryBuilder.Select(_objectiveSQLMap.TableName,
                $"{_objectiveSQLMap.Order}, {_objectiveSQLMap.Progress}")
                .Where(_playerSQLMap.UUID,uuid)
                .And(_questSQLMap.QuestTag, questTag)
                .And(_questSQLMap.StageID, stageNode.ID);

                Dictionary<int,int>? objProgress = null;

                using(var result = _mySQL.ExecuteQuery())
                {
                    if (result.HasData)
                    {
                        objProgress = result.Select(r =>
                        {
                            return new KeyValuePair<int,int>(r.Get<int>(0),r.Get<int>(1));
                        }).ToDictionary();
                    }
                }

                if(objProgress == null && stageNode.Objectives.Count > 0)
                {
                    _log.Error("Failed to load objective progress from database for stage containing objectives");
                    return false;
                }

                if(objProgress != null)
                {                
                    for(int i = 0; i < stageNode.Objectives.Count; i++)
                    {
                        var obj = stageNode.Objectives[i];
                        if(!objProgress.TryGetValue(i, out var val)){
                            _log.Error("Missing objective index: " + i.ToString());
                            continue;
                        }

                        obj.GetTrackedProgress(player)?.SetProgressValue(val);
                    }
                }

            }
            else
            {
                // _log.Info("Failed to load player quest form snapshot");
                return false;
            }

            var data = _questData[questTag][player];
            for(int i = 1; i < snapshot[snapshot[0]]; i++)
                data.PushCompletedStage(snapshot[i++]);
            
            var gender = player.ControlledCreature!.Gender;
            data.ReconstructJournal(snapshot, gender);

            return true;
        }

        public string? GetStageText(string questTag, int stageId, Gender gender)
        {
            if(!_questPackMan.TryGetStageNodeImmediate(questTag, stageId,out var stageNode))
                return "<STAGE NOT FOUND>";

            if(!stageNode.ShowInJournal) return null;

            return Regex.Replace(stageNode.JournalEntry, "<([^>/]+)/([^>]+)>", m =>
            {
                // text with token example: "Hello <boy/girl>!"
                string left  = m.Groups[1].Value;   // text before '/'
                string right = m.Groups[2].Value;   // text after '/'

                bool condition = gender == Gender.Male;

                return condition ? left : right;
            });
        }

        /// <inheritdoc cref="IQuestInterface.ClearQuest(NwPlayer, string)"/>
        public bool ClearQuest(NwPlayer player, string questTag)
        {
            // _log.Info(" - - - Clearing quest");
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IQuestInterface.CompleteQuest(NwPlayer, string, int)"/>
        public bool CompleteQuest(NwPlayer player, string questTag, int stageId = -1)
        {
            // _log.Info("Completing player quest " + questTag +" on stage "+stageId);
            if(_loadedQuests.TryGetValue(questTag, out var graph))
            {
                stageId = stageId == -1 ? graph.GetRoot(player) : stageId;
                if(stageId < 0) return false;
            }

            if(!_completedQuests.TryGetValue(player, out var dict))
            {
                dict = new();
                _completedQuests.Add(player, dict);
            }

            if(dict.TryGetValue(questTag, out var oldId))
            {
                if(oldId != stageId)
                    _log.Warn($"Overriding final stage of completed quest \'{questTag}\' from {oldId} to {stageId}");

                dict[questTag] = stageId;
            }
            else dict.Add(questTag,stageId);

            ((IQuestDatabase)this).UpdateQuest(player, questTag);

            if(graph != null && graph.RemovePlayer(player) && graph.IsEmpty)
                UnregisterQuest(questTag);

            return true;
        }




        /// <inheritdoc cref="IQuestInterface.CompleteStage(NwPlayer, string, int)"/>
        public bool CompleteStage(NwPlayer player, string questTag, int stageId = -1)
        {
            // _log.Info("Completing quest " + questTag +" stage "+stageId);

            if(!_loadedQuests.TryGetValue(questTag, out var graph))
                return false;

            var root = graph.GetRoot(player);

            if(graph.GetRoot(player) < 0) // not on the quest
                return GiveQuest(player,questTag,stageId) && CompleteStage(player,questTag,-1);
            
            if(stageId >= 0 && stageId != root)
                graph.Evaluate(stageId,player,QuestGraph.EvaluationPolicy.SkipToNextRoot);

            // graph.Evaluate(stageId,player,QuestGraph.EvaluationPolicy.SuspendOnLeaf);
            graph.Evaluate(stageId,player,QuestGraph.EvaluationPolicy.Default);

            return true;
        }

        /// <inheritdoc cref="IQuestInterface.IsOnQuest(NwPlayer, string, out int)"/>
        public bool IsOnQuest(NwPlayer player, string questTag, out int stageId)
        {
            if(_loadedQuests.TryGetValue(questTag, out var graph))
            {
                stageId = graph.GetRoot(player);
                return stageId >= 0;
            }
            stageId = -1;
            return false;
        }
        /// <inheritdoc cref="IQuestInterface.IsOnQuest(NwPlayer, string, out int)"/>
        public static bool PlayerIsOnQuest(NwPlayer player, string questTag, out int stageId) => _instance!.IsOnQuest(player,questTag, out stageId);

    
        /// <inheritdoc cref="IQuestInterface.HasCompletedQuest(NwPlayer, string, out int)"/>
        public bool HasCompletedQuest(NwPlayer player, string questTag, out int stageId)
        {
            if(!_completedQuests.TryGetValue(player, out var dict))
            {
                dict = new();
                _completedQuests.Add(player,dict);
            }

            if(dict.TryGetValue(questTag, out stageId))
                return true;

            return false;
        }

        /// <inheritdoc cref="IQuestInterface.HasCompletedQuest(NwPlayer, string, out int)"/>
        public static bool PlayerHasCompletedQuest(NwPlayer player, string questTag, out int stageId) => _instance!.HasCompletedQuest(player, questTag, out stageId);


        private bool isDisposed = false;
        public void Dispose()
        {
            // _log.Warn("DISPOSING");
            ObjectDisposedException.ThrowIf(isDisposed, this);
            isDisposed = true;

            foreach(var dict in _questData.Values)
                foreach(var p in dict.Values)
                    p.Dispose();
            
            _questData.Clear();

            foreach(var graph in _loadedQuests.Values)
                graph.Dispose();

            _loadedQuests.Clear();
            _completedQuests.Clear();

            _questPackMan.Dispose();

            _instance = null;
        }


        private sealed class QuestRowData
        {
            public readonly string QuestTag = string.Empty;
            public readonly int StageID = -1;
            public readonly bool IsCompleted = false;
            public readonly int[] Snapshot = Array.Empty<int>();

            public QuestRowData(ISqlRowData rowData)
            {
                QuestTag = rowData.Get<string>(0)!;
                StageID = rowData.Get<int>(1);
                IsCompleted = rowData.Get<bool>(2);
                string ssStr = rowData.Get<string>(3) ?? string.Empty;
                Snapshot = GetSnapshotArray(ssStr);
            }
        }

        private sealed class ObjectiveRowData
        {
            public readonly int Order = -1;
            public readonly int Progress = -1;

            public ObjectiveRowData(ISqlRowData rowData)
            {
                Order = rowData.Get<int>(0);
                Progress = rowData.Get<int>(1);
            }
        }

        public void LoadVisibilityOverrides(NwPlayer player)
        {
            if(player.IsDM) return; // QuestSystem skips DM clients entirely (it does NOT skip PlayerDMs)

            if(!TryGetUUIDStringWithErrorLog(player, out var uuid))
                return;

            var overrides = GetVisibilityOverridesFromDatabase(player);

            if(overrides == null){
                _log.Warn("Skipping ALL visibility overrides coming from Quest System for player " + player.PlayerName);
                return;
            }

            if(!_visibilityOverridesCache.TryAdd(player, overrides))
                _visibilityOverridesCache[player] = overrides;
        }

        Task LoadPlayerQuests(NwPlayer player)
        {
            // _log.Info("\n -- Loading player quests...\n");

            if(player.IsDM) return Task.CompletedTask; // QuestSystem skips DM clients entirely (it does NOT skip PlayerDMs)

            if(!TryGetUUIDStringWithErrorLog(player, out var uuid)) return Task.CompletedTask;

            var pc = player.ControlledCreature;

            if(pc == null) return Task.CompletedTask;

            QuestRowData[] questsRowData;

            // fetch all quests for this player from DB
            _mySQL.QueryBuilder.Select(_questSQLMap.TableName,
            $"{_questSQLMap.QuestTag},{_questSQLMap.StageID},{_questSQLMap.IsCompleted},{_questSQLMap.Snapshot}")
            .Where(_playerSQLMap.UUID, uuid);

            using (var result = _mySQL.ExecuteQuery())
            {
                if (result.HasData)
                    questsRowData = result.Select(row=>new QuestRowData(row)).ToArray();

                else questsRowData = Array.Empty<QuestRowData>();
            }

            foreach(var qrd in questsRowData)
            {                
                if(!_questPackMan.TryGetQuestImmediate(qrd.QuestTag, out var quest))
                {
                    _log.Error("Quest " + qrd.QuestTag + " not found in packs");
                    continue;
                }

                if(qrd.Snapshot.Length <= 2) throw new InvalidOperationException("Corrupted snapshot");

                // load from snapshot if the quest is active
                if (!qrd.IsCompleted)
                {
                    if(!LoadFromSnapshot(player, qrd.QuestTag, qrd.Snapshot))
                        _log.Error("Active quest " + qrd.QuestTag + " failed to load from snapshot");
                    
                    continue;
                }

                // otherwise load only journal text if the quest is completed

                int footprintsStart = qrd.Snapshot[0];

                string journalText = string.Empty;
                var gender = pc.Gender;
                // update journal for completed quests
                for(int i = 1; i <= footprintsStart ; i++)
                {
                    var nodeID = qrd.Snapshot[i];
                    i++;

                    if(!_questPackMan.TryGetStageNodeImmediate(qrd.QuestTag, nodeID, out var stageNode))
                    {
                        _log.Error("Quest " + qrd.QuestTag + " does not have stage node with ID " + nodeID.ToString());
                        continue;
                    }
                    var stageEntry = GetStageText(qrd.QuestTag,nodeID, gender);

                    if(string.IsNullOrEmpty(stageEntry)) continue;
                    
                    string str = stageEntry + "\n\n\n" + PlayerJournalState.StageSeparatorString;
                    // _log.Info($"retrieved stage {nodeID} text:\n{str}");
                    journalText += str;
                }

                var entry = new NWN.Core.NWNX.JournalEntry()
                {
                    sTag = quest.Tag,
                    sName = quest.Name,
                    nUpdated = 1,
                    nQuestDisplayed = 1,
                    nQuestCompleted = 1,
                    nState = 0,
                    nPriority = 0,
                    sText = journalText
                };

                // _log.Info("Completed quest entry retrieved:\n"+journalText);
                NWN.Core.NWNX.PlayerPlugin.AddCustomJournalEntry(player.ControlledCreature!.ObjectId, entry, 1);
            }

            return Task.CompletedTask;
        }
        void IQuestDatabase.LazyLoadPlayerQuests(NwPlayer player) // TODO: make it actually LAZY, and split between delays
        {
            // _log.Info("\n -- Lazy loading player quests...\n");

            _ = NwTask.Run(async ()=>
            {
                try
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(2));
                    await NwTask.SwitchToMainThread();
                    await LoadPlayerQuests(player);
                }
                catch (Exception ex)
                {
                    _log.Error("Exception from async task: " + ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        void UpdateCompletedQuest(NwPlayer player, string questTag, int[] snapshot)
        {            
            // _log.Info("\n -- Updating player completed quest in database (WIP)\n");

            if(!TryGetGraphWithErrorLog(questTag, out var graph) ||
                !TryGetUUIDStringWithErrorLog(player, out var uuid) ||
                !TryCaptureSnapshotWithErrorLog(graph,player, out var ss))
                return;

            var ssStr = GetSnapshotString(snapshot);

            if(_completedQuests.TryGetValue(player, out var dict) && dict.TryGetValue(questTag, out var value))
            {
                if(InsertOrUpdateQuestInDatabase(uuid,questTag, true, ssStr, value))
                {
                    // _log.Info("Completed quest written to database.");
                }
            }
        }

        bool InsertOrUpdateQuestInDatabase(string uuid, string questTag, bool isCompleted, string snapshotString, int stageID)
        {
            // select count to know whether to Insert or Update
            _mySQL.QueryBuilder.SelectCount(_questSQLMap.TableName)
            .Where(_playerSQLMap.UUID, uuid)
            .And(_questSQLMap.QuestTag, questTag);

            int count = _mySQL.ExecuteQuery().Rows;

            if(count == 0) // player has no data for this quest in DB -> Insert
            {
                _mySQL.QueryBuilder.InsertInto(_questSQLMap.TableName,
                $"{_playerSQLMap.UUID}, {_questSQLMap.QuestTag}, {_questSQLMap.IsCompleted}, {_questSQLMap.Snapshot}, {_questSQLMap.StageID}",
                uuid,questTag,(isCompleted ? 1 : 0),snapshotString,stageID);

                count = _mySQL.ExecuteQuery().Rows;

                if(count != 1)
                {                    
                    _log.Error("Failed to insert quest data into DB");
                    return false;
                }
            }
            else if(count == 1) // player has data for this quest in DB -> Update
            {
                // remove every objective from this quest-objectives db table
                _mySQL.QueryBuilder.DeleteFrom(_objectiveSQLMap.TableName)
                .Where(_playerSQLMap.UUID,uuid)
                .And(_questSQLMap.QuestTag,questTag);

                _ = _mySQL.ExecuteQuery();

                // update quest data
                _mySQL.QueryBuilder.Update
                (
                    _questSQLMap.TableName,
                    $"{_questSQLMap.IsCompleted}, {_questSQLMap.Snapshot}, {_questSQLMap.StageID}",
                    (isCompleted ? 1 : 0), snapshotString, stageID
                )
                .Where(_playerSQLMap.UUID, uuid)
                .And(_questSQLMap.QuestTag, questTag)
                .Limit(1);

                count = _mySQL.ExecuteQuery().Rows;
                if(count != 1) 
                {
                    _log.Error("Failed to update quest data in DB");
                    return false;
                }
            }
            else // error occurred, 
            {
                string errorStr = count < 0 ? 
                "Failed to check whether the player has quest data in database." : 
                $"{count} entries of the same quest {questTag} for PC uuid '{uuid}' found in database!";

                _log.Error(errorStr);
                return false;
            }

            return true;
        }
        
        void IQuestDatabase.UpdateQuest(NwPlayer player, string questTag)
        {
            // _log.Info("\n -- Updating player active quest in database (WIP)\n");

            if(!TryGetGraphWithErrorLog(questTag, out var graph) ||
                !TryGetUUIDStringWithErrorLog(player, out var uuid) ||
                !TryCaptureSnapshotWithErrorLog(graph,player, out var ss))
                return;

            var ssStr = GetSnapshotString(ss);
            if(graph.GetRootNode(player) is not StageNodeWrapper rootNode)
            {
                _log.Error("No root stage node");
                return;
            }

            if(!InsertOrUpdateQuestInDatabase(uuid, questTag, false, ssStr, rootNode.ID))
                return;

            // write objectives for the current stage
            // _log.Info($"Writing {rootNode.Objectives.Count} objectives to database");
            for(int i = 0; i < rootNode.Objectives.Count; i++)
            {
                var obj = rootNode.Objectives[i];
                if(!TryGetObjectiveProgressValueWithErrorLog(obj, player, out var progressValue))
                    return;

                _mySQL.QueryBuilder.InsertInto(_objectiveSQLMap.TableName,
                $"{_playerSQLMap.UUID}, {_questSQLMap.QuestTag}, {_questSQLMap.StageID}, {_objectiveSQLMap.Order}, {_objectiveSQLMap.Progress}",
                uuid,questTag,rootNode.ID,i,progressValue);

                _mySQL.ExecuteQuery();
            }

            // _log.Info("Active quest written to database.");
            
        }

        void IQuestDatabase.ClearQuest(NwPlayer player, string questTag)
        {
            // _log.Info("\n -- Clearing player quest from database (WIP)\n");

            if(!TryGetUUIDStringWithErrorLog(player, out var uuid)) return;

            // first, remove visibility overrides
            _mySQL.QueryBuilder.DeleteFrom(_visibilitySQLMap.TableName).Where(_playerSQLMap.UUID,uuid).And(_questSQLMap.QuestTag, questTag);

            // then remove objectives
            _mySQL.QueryBuilder.DeleteFrom(_objectiveSQLMap.TableName).Where(_playerSQLMap.UUID,uuid).And(_questSQLMap.QuestTag, questTag);
            _mySQL.ExecuteQuery();

            // remove quest at the very end
            _mySQL.QueryBuilder.DeleteFrom(_questSQLMap.TableName).Where(_playerSQLMap.UUID,uuid).And(_questSQLMap.QuestTag, questTag);
            _mySQL.ExecuteQuery();
            
            // _log.Info("Quest cleared from database");
        }

        void IQuestDatabase.UpdateProgress(NwPlayer player, ObjectiveWrapper objective, string questTag, int stageID)
        {
            // _log.Info("\n -- Updating player quest objective progress in database (WIP)\n");


            if(!TryGetGraphWithErrorLog(questTag, out var graph) ||
                !TryGetStageNodeWithErrorLog(graph, player, out var node) ||
                !TryGetUUIDStringWithErrorLog(player, out var uuid) ||
                !TryGetObjectiveProgressValueWithErrorLog(objective,player,out var progressValue))
                return;

            int order = -1;
            for(int i = 0; i < node.Objectives.Count; i++){
                var o = node.Objectives[i];
                if(o == objective)
                {
                    order = i;
                    break;
                }
            }

            if(order < 0)
            {
                _log.Error("Objective not found in stage objectives");
                return;
            }

            _mySQL.QueryBuilder.Update(_objectiveSQLMap.TableName, _objectiveSQLMap.Progress, progressValue)
            .Where(_playerSQLMap.UUID,uuid)
            .And(_questSQLMap.QuestTag, questTag)
            .And(_questSQLMap.StageID, stageID)
            .And(_objectiveSQLMap.Order, order)
            .Limit(1);

            _mySQL.ExecuteQuery();

            // _log.Info("Objective progress updated in database");
        }

        void IQuestDatabase.ClearProgress(NwPlayer player, int objectiveIndex, string questTag, int stageID)
        {
            // _log.Info("\n -- Clearing player quest objective progress from database (WIP)\n");

            if(!TryGetUUIDStringWithErrorLog(player, out var uuid)) return;

            _mySQL.QueryBuilder.DeleteFrom(_objectiveSQLMap.TableName)
            .Where(_playerSQLMap.UUID,uuid).And(_questSQLMap.QuestTag,questTag).And(_questSQLMap.StageID, stageID).And(_objectiveSQLMap.Order, objectiveIndex);

            _mySQL.ExecuteQuery();

            // _log.Info("Objective progress cleared from database");
        }

        void IQuestDatabase.UpdateStageProgress(NwPlayer player, StageNodeWrapper stage)
        {
            // _log.Info("\n -- Updating player quest stage progress in database (WIP)\n");
            
            ((IQuestDatabase)this).UpdateQuest(player, stage.Quest!.Tag);
        }

        void IQuestDatabase.ClearStageProgress(NwPlayer player, string questTag, int stageID)
        {
            // _log.Info("\n -- Clearing player quest stage progress from database (WIP)\n");

            if(!TryGetUUIDStringWithErrorLog(player, out var uuid)) return;

            _mySQL.QueryBuilder.DeleteFrom(_objectiveSQLMap.TableName)
            .Where(_playerSQLMap.UUID, uuid).And(_questSQLMap.QuestTag, questTag).And(_questSQLMap.StageID, stageID);
            
            _mySQL.ExecuteQuery();

            // _log.Info("Stage progress cleared from database");
        }


        void OnPlayerDataReadyForUpdate(NwPlayer player, string questTag)
        {
            // _log.Info("\n -- Lazy autoupdate\n");

            ((IQuestDatabase)this).UpdateQuest(player, questTag);

        }

        
        ///////////////////////////////////
        ///////////////////////////////////
        /// HELPERS
        /// 
        
        static bool TryCaptureSnapshotWithErrorLog(QuestGraph graph, NwPlayer player, [NotNullWhen(true)] out int[]? snapshot)
        {
            snapshot = graph.CaptureSnapshot(player);
            if(snapshot == null)
            {
                _log.Error("Failed to capture snapshot");
                return false;
            }
            return true;
        }

        bool TryGetGraphWithErrorLog(string questTag, [NotNullWhen(true)] out QuestGraph? graph)
        {            
            if(!_loadedQuests.TryGetValue(questTag, out graph))
            {
                _log.Error("Can't save quest state, while it is not loaded into memory");
                return false;
            }
            return true;
        }

        static bool TryGetUUIDStringWithErrorLog(NwPlayer player, [NotNullWhen(true)] out string? uuid)
        {
            uuid = null;
            
            if(!player.IsValid) {
                _log.Error("Invalid player");
                return false;
            }

            var pc = player.ControlledCreature;

            if(pc == null || !pc.IsValid || !pc.TryGetUUID(out var guid)){
             
                _log.Error("Failed to get UUID string from PC");
                return false;
            }
            uuid = guid.ToUUIDString();
            return true;
        }

        static bool TryGetStageNodeWithErrorLog(QuestGraph graph, NwPlayer player, [NotNullWhen(true)] out StageNodeWrapper? stageNodeWrapper)
        {
            
            if (graph.GetRootNode(player) is not StageNodeWrapper node)
            {
                stageNodeWrapper = null;
                _log.Error("No stage root node");
                return false;
            }
            stageNodeWrapper = node;
            return true;
        }

        static bool TryGetObjectiveProgressValueWithErrorLog(ObjectiveWrapper objective, NwPlayer player, out int value)
        {
            var progress = objective.GetTrackedProgress(player);
            if(progress == null)
            {
                _log.Error("Objective is not tracking this player");
                value = -1;
                return false;
            }
            value = progress.GetProgressValue();
            return true;
        }

        static string GetSnapshotString(int[] snapshot) => string.Join(',',snapshot.Select(i=>i.ToString()));
        static int[] GetSnapshotArray(string snapshotString)
        {
            var splitStr = snapshotString.Split(',');
            return splitStr.Select(s=>int.Parse(s)).ToArray();
        }


        public void CombineVisibility(NwPlayer player, string questTag, int nodeID)
        {
            if(!TryGetUUIDStringWithErrorLog(player, out var uuid) ||
                !TryGetGraphWithErrorLog(questTag, out var graph))
                return;

            var node = graph.GetNode<VisibilityNodeWrapper>(nodeID)?.Node ?? _questPackMan.GetNodeImmediate<VisibilityNode>(questTag, nodeID);

            if(node == null)
            {
                _log.Error($"Quest \'{questTag}\' node {nodeID} does not exist");
                return;
            }

            _mySQL.QueryBuilder.Select(_visibilitySQLMap.TableName,_visibilitySQLMap.VisibilityOverrides)
            .Where(_playerSQLMap.UUID,uuid)
            .And(_questSQLMap.QuestTag, questTag)
            .Limit(1);

            Dictionary<string,bool> existingOverrides = new();
            var cache = _visibilityOverridesCache[player];

            string str = string.Empty;

            bool update = false;

            using (var result = _mySQL.ExecuteQuery())
            {
                if (result.HasData)
                {
                    update = true;
                    var row = result.First();
                    str = row.Get<string>(0) ?? string.Empty;

                    var pairs = str.Split(',');
                    
                    foreach(var pair in pairs)
                    {
                        var split = pair.Split("::");
                        var visible = int.Parse(split[1]) > 0;

                        if(!existingOverrides.TryAdd(split[0], visible))
                            existingOverrides[split[0]] = visible;

                        if(!cache.TryAdd(split[0], visible))
                            cache[split[0]] = visible;
                    }
                }
            }

            foreach(var kvp in node.Objects)
                if(existingOverrides.TryAdd(kvp.Key,kvp.Value))
                    str += $"{(str.Length > 0 ? ',' : string.Empty)}{kvp.Key}::{(kvp.Value ? 1 : 0)}";

            if (update)
            {
                _mySQL.QueryBuilder.Update(
                    _visibilitySQLMap.TableName,
                    _visibilitySQLMap.VisibilityOverrides,
                    str
                )
                .Where(_playerSQLMap.UUID,uuid)
                .And(_questSQLMap.QuestTag,questTag)
                .Limit(1);
            }
            else
            {            
                _mySQL.QueryBuilder.InsertInto(
                    _visibilitySQLMap.TableName,
                    $"{_playerSQLMap.UUID},{_questSQLMap.QuestTag},{_visibilitySQLMap.VisibilityOverrides}",
                    uuid,questTag,str
                );
            }

            _mySQL.ExecuteQuery();
        }
    

        private Dictionary<string,bool>? GetVisibilityOverridesFromDatabase(NwPlayer player)
        {
            if(!TryGetUUIDStringWithErrorLog(player, out var uuid))
                return null;

            _mySQL.QueryBuilder.Select(_visibilitySQLMap.TableName,_visibilitySQLMap.VisibilityOverrides)
            .Where(_playerSQLMap.UUID,uuid);

            Dictionary<string,bool> dict = new();

            using var res = _mySQL.ExecuteQuery();

            if (!res.HasData) return new();

            foreach (var r in res)
            {
                var str = r.Get<string>(0)!;
                var kvps = str.Split(',');
                foreach (var kvp in kvps)
                {
                    var split = kvp.Split("::");
                    bool val = int.Parse(split[1]) > 0;
                    if(!dict.TryAdd(split[0],val))
                        dict[split[0]] = val;
                }
            }

            return dict;
        }

        private static (string,string) SplitKey(string key)
        {
            var s = key.Split(':');
            return (s[0],s.Length > 0 ? s[1] : string.Empty);
        }

        private Dictionary<NwPlayer, Dictionary<string,bool>> _visibilityOverridesCache = new();

        private static bool MatchResRefAndTag(NwObject obj, (string,string) splitKey)
        {
            return obj.ResRef == splitKey.Item1 && (obj.Tag == splitKey.Item2 || splitKey.Item2 == string.Empty);
        }

        public void RefreshVisibilityOverridesForPlayerEnteringArea(NwPlayer player, NwArea area)
        {
            if(!_visibilityOverridesCache.TryGetValue(player, out var overrides))
                return;
                
            foreach(var kvp in overrides)
            {
                var splitKey = SplitKey(kvp.Key);

                foreach(var o in area.Objects.Where(o=>o.IsValid && MatchResRefAndTag(o,splitKey)))
                    player.SetPersonalVisibilityOverride(o, kvp.Value ? Anvil.Services.VisibilityMode.Visible : Anvil.Services.VisibilityMode.Hidden);

            }
        }

        public void RefreshVisibilityOverridesForObjectEnteringArea(NwGameObject obj, NwArea area)
        {
            if(area.PlayerCount == 0) return;

            foreach(var creature in area.FindObjectsOfTypeInArea<NwCreature>().Where(c=>c.IsValid && c.ControllingPlayer != null))
            {
                if(!creature.IsPlayerControlled(out var player) || player.IsDM)
                    continue;
                    
                if(!_visibilityOverridesCache.TryGetValue(player, out var overrides))
                    continue;

                foreach(var kvp in overrides)
                {
                    var splitKey = SplitKey(kvp.Key);
                    if(MatchResRefAndTag(obj,splitKey))
                    {
                        player.SetPersonalVisibilityOverride(obj, kvp.Value ? Anvil.Services.VisibilityMode.Visible : Anvil.Services.VisibilityMode.Hidden);
                        break;
                    }
                }
            }
        }
    
    }
}