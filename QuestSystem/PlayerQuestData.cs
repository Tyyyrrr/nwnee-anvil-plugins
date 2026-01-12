using System;
using System.Collections.Generic;
using Anvil.API;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    internal sealed class PlayerQuestData : IDisposable
    {
        private readonly QuestWrapper _wrapper;
        public QuestStageWrapper? CurrentStage => _chain.TryPeek(out var lastStageId) ? _wrapper[lastStageId] : null;

        private readonly NwPlayer _player;
        private readonly PlayerJournalState _journalState;

        private readonly Stack<int> _chain = new();

        private bool _isQuestCompleted = false;
        public bool IsQuestCompleted 
        {
            get => _isQuestCompleted;
            set
            {
                if(_isQuestCompleted || !value) return;
                _isQuestCompleted = true;
                _journalState.MarkCompleted(_player, _wrapper);
            }
        }

        private bool _isStageCompleted = false;
        public bool IsStageCompleted
        {
            get => _isStageCompleted;
            set
            {
                if(_isStageCompleted || !value) return;
                _isStageCompleted = true;
            }
        }

        public PlayerQuestData(NwPlayer player, QuestWrapper wrapper) 
        {
            _player = player;
            _wrapper = wrapper;
            _journalState = new();
            _journalState.JournalReady += OnJournalReadyForUpdate;
        }

        public void Update()
        {
            _journalState.ScheduleUpdate();
        }

        public void ResetCurrentStageProgress()
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - - - Resetting current stage progress");
            if(CurrentStage == null) return;
            CurrentStage.StopTracking(_player);
            CurrentStage.TrackProgress(_player);
            _journalState.ScheduleUpdate();
        }


        public void PushStage(QuestStageWrapper stage)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - - - Pushin' stage");
            if(stage == null) 
                return;

            if(stage == CurrentStage)
            {
                ResetCurrentStageProgress();
                return;
            }

            _chain.Push(stage.ID);
            _journalState.PushEntry(stage.ID, stage.GetStageJournalEntry(_player));
        }

        void OnJournalReadyForUpdate()
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - - - Journal ready for update");
            _journalState.Update(_player,_wrapper);
        }

        public void Dispose()
        {
            NLog.LogManager.GetCurrentClassLogger().Warn("DISPOSING");
            _journalState.JournalReady -= OnJournalReadyForUpdate;
            _journalState.Dispose();
        }
    }
}