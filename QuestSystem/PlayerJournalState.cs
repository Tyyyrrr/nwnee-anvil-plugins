using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Anvil.API;

namespace QuestSystem
{
    internal sealed class PlayerJournalState : IDisposable
    {
        private static readonly string StageSeparatorString = "____________________\n\n";

        private static readonly string ObjectiveSeparatorString = "--------------------------------\n";

        public int LastStageID => _offsets.TryPeek(out var offset) ? offset.ID : -1;

        private int totalLength = 0;

        private readonly Stack<JournalOffset> _offsets = new(10);
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
                    await NwTask.Delay(TimeSpan.FromSeconds(0.41291), _cts.Token);
                    if (!_cts.IsCancellationRequested)
                        JournalReady?.Invoke();
                }
                catch (OperationCanceledException)
                {
                    // expected during disposal
                }
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
        

        public void PushEntry(int stageId, string? text)
        {
            ScheduleUpdate();

            if (!string.IsNullOrEmpty(text))
            {
                var txt = StageSeparatorString + text + "\n\n\n";
                _schedule.Enqueue(txt);
                _offsets.Push(new(stageId, txt.Length));
            }
            _offsets.Push(new(stageId,0));
        }


        public void Update(NwPlayer player, string tag)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(" - - - - - Updating journal");

            var entry = player.GetJournalEntry(tag);
            var text = entry?.Text ?? string.Empty;

            text = text.Length > totalLength ? text[..totalLength] : text;


            var stage = wrapper[LastStageID];

            var objStr = stage?.GetObjectivesJournalEntry(player);
            if(!string.IsNullOrEmpty(objStr))
                objStr = ObjectiveSeparatorString + objStr;

            entry ??= new();

            entry.Text = string.Concat(text,string.Concat(_schedule),objStr);
            entry.QuestTag = wrapper.Tag;
            entry.QuestCompleted = false;
            entry.QuestDisplayed = entry.Text.Length > 0;
            entry.Updated = true;
            entry.Name = wrapper.Name;
            
            totalLength = entry.Text - objStr.Length;

            player.AddCustomJournalEntry(entry, entry.Text.Length == 0);
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
            
            entry.QuestCompleted = true;
            entry.Text = entry.Text[.._offsets.Sum(o=>o.Length)];
            entry.Updated = true;
            entry.QuestCompleted = true;

            player.AddCustomJournalEntry(entry);
        }
    }
}