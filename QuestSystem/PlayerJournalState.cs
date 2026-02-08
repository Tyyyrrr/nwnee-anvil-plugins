using System;
using System.Collections.Generic;
using System.Threading;
using Anvil.API;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem
{
    internal sealed class PlayerJournalState : IDisposable
    {
        private static readonly string StageSeparatorString = "____________________\n\n";

        private static readonly string ObjectiveSeparatorString = "--------------------------------\n";

        private int stagesTextLength = 0;
        public bool SilentUpdate {get;set;} = false;

        private readonly Queue<string> _schedule = new(10);
        private readonly CancellationTokenSource _cts = new();

        private readonly struct JournalOffset
        {
            public readonly int ID;
            public readonly int Length;
            
            public JournalOffset(int stageID, int stageLength)
            {
                ID = Math.Max(0,stageID);
                Length = Math.Max(0,stageLength);
            }
        }

        public event System.Action? JournalReady;

        bool scheduled = false;
        public void ScheduleUpdate()
        {
            if (scheduled) return;
            scheduled = true;
            NLog.LogManager.GetCurrentClassLogger().Info(" - - - - - Update scheduled");

            _ = NwTask.Run(async () =>
            {
                try
                {
                    await NwTask.Delay(TimeSpan.FromSeconds(0.3291), _cts.Token);
                    if (!_cts.IsCancellationRequested)
                        JournalReady?.Invoke();
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
        

        public void PushEntry(string? text)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn("JOURNAL PUSH ENTRY: " + text +", currently scheduled stages: " + _schedule.Count);
            ScheduleUpdate();

            if (!string.IsNullOrEmpty(text))
            {
                var txt = StageSeparatorString + text + "\n\n\n";
                _schedule.Enqueue(txt);                
            }
        }


        public void Update(NwPlayer player, Quest quest, StageNodeWrapper stage)
        {
            var entry = player.GetJournalEntry(quest.Tag);
            var text = entry?.Text ?? string.Empty;

            text = text[..stagesTextLength];


            var objStr = stage?.GetObjectivesJournalEntry(player);

            if(!string.IsNullOrEmpty(objStr))
                objStr = ObjectiveSeparatorString + objStr;

            entry ??= new();

            entry.Text = string.Concat(text,string.Concat(_schedule),objStr);
            _schedule.Clear();
            entry.QuestTag = quest.Tag;
            entry.QuestCompleted = false;
            entry.QuestDisplayed = entry.Text.Length > 0;
            entry.Updated = true;
            entry.Name = quest.Name;
            entry.Priority = 0;
            entry.State = 0;
            player.AddCustomJournalEntry(entry, SilentUpdate);
            SilentUpdate = false;
            stagesTextLength = entry.Text.Length - (objStr?.Length ?? 0);
        }


        public void Dispose()
        {
            NLog.LogManager.GetCurrentClassLogger().Warn("DISPOSING");
            _cts.Cancel();
            _cts.Dispose();
        }

        public void MarkCompleted(NwPlayer player, string tag)
        {
            var entry = player.GetJournalEntry(tag) 
                ?? throw new InvalidOperationException("There is no Journal Quest Entry to complete");
            
            NLog.LogManager.GetCurrentClassLogger().Warn("MARKING QUEST AS COMPLETED");
            NLog.LogManager.GetCurrentClassLogger().Info("Full entry: " + entry.Text);
            NLog.LogManager.GetCurrentClassLogger().Info("full entry length: " + entry.Text.Length + ", stages length: " + stagesTextLength);
            NLog.LogManager.GetCurrentClassLogger().Info("Cut entry: " + entry.Text[..stagesTextLength]);

            entry.QuestCompleted = true;
            entry.Text = entry.Text[..stagesTextLength];
            entry.Priority = 0;
            entry.State = 0;
            entry.Updated = true;
            entry.QuestCompleted = true;
            entry.QuestDisplayed = true;

            player.AddCustomJournalEntry(entry);
        }
    }
}