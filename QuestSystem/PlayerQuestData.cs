using System;
using System.Collections.Generic;
using Anvil.API;
using QuestSystem.Graph;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem
{
    internal sealed class PlayerQuestData : IDisposable
    {
        private readonly NwPlayer _player;
        private readonly PlayerJournalState _journalState;

        private readonly Stack<int> _chain = new();

        private readonly QuestGraph _graph;

        // private bool _isQuestCompleted = false;
        // public bool IsQuestCompleted 
        // {
        //     get => _isQuestCompleted;
        //     set
        //     {
        //         if(_isQuestCompleted || !value) return;
        //         _isQuestCompleted = true;
        //         _journalState.MarkCompleted(_player, _graph.Quest.Tag);
        //     }
        // }

        // private bool _isStageCompleted = false;
        // public bool IsStageCompleted
        // {
        //     get => _isStageCompleted;
        //     set
        //     {
        //         if(_isStageCompleted || !value) return;
        //         _isStageCompleted = true;
        //     }
        // }

        public PlayerQuestData(NwPlayer player, QuestGraph graph) 
        {
            _graph = graph;
            _player = player;
            _journalState = new();
            _journalState.JournalReady += OnJournalReadyForUpdate;
        }

        public void Update()
        {
            _journalState.ScheduleUpdate();
        }


        public void PushStage(StageNodeWrapper stage)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - - - Pushin' stage " + stage.ID);

            var current = _graph.GetRootNode(_player) as StageNodeWrapper ?? throw new InvalidOperationException("Player is not on the graph");

            if (_chain.TryPeek(out var previousStageID) && previousStageID == current.ID)
                current.Reset(_player);

            else _chain.Push(stage.ID);

            _journalState.PushEntry(stage.GetStageJournalEntry(_player));
        }

        void OnJournalReadyForUpdate()
        {
            if (_graph.GetRootNode(_player) is StageNodeWrapper currentStage)
                _journalState.Update(_player, _graph.Quest, currentStage);
        }

        public void Dispose()
        {
            NLog.LogManager.GetCurrentClassLogger().Warn("DISPOSING");
            _journalState.JournalReady -= OnJournalReadyForUpdate;
            _journalState.Dispose();
        }

        public void SilentNextJournalUpdate()
        {
            _journalState.SilentUpdate = true;
        }
        public void CompleteAtStage(StageNodeWrapper stage)
        {
            _journalState.Update(_player,stage.Quest!, stage);
            _journalState.MarkCompleted(_player, stage.Quest!.Tag);
        }
    }
}