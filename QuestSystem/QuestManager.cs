using System;
using System.Collections.Generic;

using Anvil.API;
using NLog;

using MySQLClient;

using QuestSystem.Graph;
using QuestSystem.Wrappers.Nodes;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem
{
    internal sealed class QuestManager : IDisposable, IQuestInterface, IQuestDatabase
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly MySQLService _mySQL;

        private readonly QuestPackManager _questPackMan;
        
        private readonly INodeLoader _nodeLoader;

        private readonly Dictionary<string, QuestGraph>  _loadedQuests = new();
        private readonly Dictionary<NwPlayer, Dictionary<string, int>> _completedQuests = new();


        private static readonly Dictionary<string, Dictionary<NwPlayer, PlayerQuestData>>  _questData = new();
        public static PlayerQuestData? GetPlayerQuestData(NwPlayer player, Quest? quest)
        {
            if(quest == null) return null;
            return _questData.TryGetValue(quest.Tag, out var dict) && dict.TryGetValue(player, out var data) ? data : null;
        }


        public QuestManager(string questPackDirectory, MySQLService mySQL)
        {
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
            var graph = new QuestGraph(quest, _nodeLoader);
            _loadedQuests.Add(quest.Tag, graph);
            _questData.Add(quest.Tag, new());
        }

        private void UnregisterQuest(string tag)
        {
            var data = _questData[tag];
            _questData.Remove(tag);
            data.Values.DisposeAll();

            var graph = _loadedQuests[tag];
            _loadedQuests.Remove(tag);
            graph.Dispose();

            List<NwPlayer> toRemove = new();
            foreach(var kvp in _completedQuests)
            {
                _ = kvp.Value.Remove(tag);
                if(kvp.Value.Count == 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach(var p in toRemove) _completedQuests.Remove(p);
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
            ((IQuestDatabase)this).UpdateQuest(player, tag);

            var graph = _loadedQuests[tag];

            graph.RemovePlayer(player);

            if(graph.IsEmpty)
                UnregisterQuest(tag);
        }


        /// <inheritdoc cref="IQuestInterface.GiveQuest"/>
        public bool GiveQuest(NwPlayer player, string questTag, int stageId)
        {
            if(!_loadedQuests.TryGetValue(questTag, out var graph))
            {
                if(!_questPackMan.TryGetQuestImmediate(questTag, out var quest))
                {
                    _log.Error("Failed to set player on quest. The quest was not found in any packs");
                    return false;
                }

                RegisterQuest(quest);
            }

            graph = _loadedQuests[questTag];

            return graph.AddPlayer(player, stageId);
        }

        /// <inheritdoc cref="IQuestInterface.ClearQuest(NwPlayer, string)"/>
        public bool ClearQuest(NwPlayer player, string questTag)
        {
            _log.Info(" - - - Clearing quest");
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IQuestInterface.CompleteQuest(NwPlayer, string, int)"/>
        public bool CompleteQuest(NwPlayer player, string questTag, int stageId = -1)
        {
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

            if(graph != null && graph.RemovePlayer(player) && graph.IsEmpty)
                UnregisterQuest(questTag);

            ((IQuestDatabase)this).UpdateQuest(player, questTag);

            return true;
        }




        /// <inheritdoc cref="IQuestInterface.CompleteStage(NwPlayer, string, int)"/>
        public bool CompleteStage(NwPlayer player, string questTag, int stageId = -1)
        {
            if(!_loadedQuests.TryGetValue(questTag, out var graph))
                return false;

            var root = graph.GetRoot(player);

            if(graph.GetRoot(player) < 0) // not on the quest
                return GiveQuest(player,questTag,stageId) && CompleteStage(player,questTag,-1);
            
            if(stageId >= 0 && stageId != root)
                graph.Evaluate(stageId,player,QuestGraph.EvaluationPolicy.SkipToNextRoot);

            graph.Evaluate(stageId,player,QuestGraph.EvaluationPolicy.SuspendOnLeaf);

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

            foreach(var dict in _questData.Values)
                foreach(var p in dict.Values)
                    p.Dispose();
            
            _questData.Clear();

            foreach(var graph in _loadedQuests.Values)
                graph.Dispose();

            _loadedQuests.Clear();
            _completedQuests.Clear();

            _questPackMan.Dispose();
        }

        void IQuestDatabase.LazyLoadPlayerQuests(NwPlayer player)
        {
            _log.Info(" -- Lazy loading player quests (fake)");
        }

        void IQuestDatabase.UpdateQuest(NwPlayer player, string questTag)
        {
            _log.Info(" -- Updating player quest in database (fake)");
            //save completed quest
            if(_completedQuests.TryGetValue(player, out var dict) && dict.TryGetValue(questTag, out var value))
            {
                // todo: 
                // - write completed quest to db
                // - cleanup 'active' quest state from db if any
                //...
                return;
            }

            if(!_loadedQuests.TryGetValue(questTag, out var graph))
                throw new InvalidOperationException("Can't save quest state, while it is not loaded into memory");

            //save active quest
            var ss = graph.CaptureSnapshot(player);
            if(ss == null) return;
            // todo: write array to db
            // ...
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

        void IQuestDatabase.UpdateStageProgress(NwPlayer player, StageNodeWrapper stage)
        {
            _log.Info(" -- Updating player quest stage progress in database (fake)");
        }

        void IQuestDatabase.ClearStageProgress(NwPlayer player, StageNodeWrapper stage)
        {
            _log.Info(" -- Clearing player quest stage progress from database (fake)");
        }
    }
}