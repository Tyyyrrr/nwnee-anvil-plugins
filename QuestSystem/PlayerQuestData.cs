using System;
using System.Collections.Generic;
using System.Threading;
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

        private readonly CancellationTokenSource _cts = new();

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
            ScheduleUpdate();
        }

        public void ReconstructJournal(int[] snapshot, Gender gender = Gender.Male)
        {
            // var log = NLog.LogManager.GetCurrentClassLogger();

            // log.Info("Reconstructing journal. Chain length: " + _chain.Count);

            var qm = QuestManager.Instance;
            string qTag = _graph.Quest.Tag;

            var fpStart = snapshot[0];
            _journalState.Clear(_player, _graph.Quest.Tag);
            for(int i = 1; i <= fpStart; i++)
            {
                int nodeID = snapshot[i];
                i++;
                // log.Info("Reconstructing node " + nodeID);
                var text = qm.GetStageText(qTag,nodeID, gender);
                if(text != null)
                    _journalState.PushEntry(text);
                // log.Info("Reconstructed node text: " + (text ?? "<<NULL>>"));
            }
        }


        public static event Action<NwPlayer, string>? DataReady;
        bool scheduled = false;
        public void ScheduleUpdate()
        {
            if (scheduled) return;
            scheduled = true;

            _ = NwTask.Run(async () =>
            {
                try
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(2), _cts.Token);
                    await NwTask.SwitchToMainThread();
                    if (!_cts.IsCancellationRequested)
                        DataReady?.Invoke(_player, _graph.Quest.Tag);
                }

                // expected during disposal
                catch (OperationCanceledException){}
                catch(ObjectDisposedException){}

                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error("Exception from async task: " + ex.Message + "\n" + ex.StackTrace);
                }

                finally
                {
                    scheduled = false;
                }
            });
        }

        public void PushStage(StageNodeWrapper stage)
        {
            // NLog.LogManager.GetCurrentClassLogger().Info(" - - - - Pushin' stage " + stage.ID);

            if (_graph.GetRootNode(_player) is StageNodeWrapper current && _chain.TryPeek(out var previousStageID) && previousStageID == current.ID)
                current.Reset(_player);

            else _chain.Push(stage.ID);

            _journalState.PushEntry(stage.GetStageJournalEntry(_player));
            ScheduleUpdate();
        }

        public void PushCompletedStage(int stageID) => _chain.Push(stageID);
        

        void OnJournalReadyForUpdate()
        {
            if (_graph.GetRootNode(_player) is StageNodeWrapper currentStage)
                _journalState.Update(_player, _graph.Quest, currentStage);
        }

        public void Dispose()
        {
            // NLog.LogManager.GetCurrentClassLogger().Warn("DISPOSING");
            _cts.Cancel();
            _cts.Dispose();
            _journalState.JournalReady -= OnJournalReadyForUpdate;
            _journalState.Dispose();
        }

        public void SilentNextJournalUpdate()
        {
            _journalState.SilentUpdate = true;
        }
        public void CompleteAtStage(StageNodeWrapper stage)
        {
            _journalState.Update(_player,stage.Quest!, stage, true);
        }
    }
}