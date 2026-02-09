using System;
using System.Text;
using System.Threading;
using Anvil.API;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem
{
    internal sealed class PlayerJournalState : IDisposable
    {
        private static readonly string StageSeparatorString = "____________________\n\n";

        private static readonly string ObjectiveSeparatorString = "--------------------------------\n";

        private readonly StringBuilder _stringBuilder = new();
        public bool SilentUpdate {get;set;} = false;

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
            ScheduleUpdate();

            if (!string.IsNullOrEmpty(text))
            {
                var txt = StageSeparatorString + text + "\n\n\n";
                _stringBuilder.Append(txt);
            }
        }


        private int nEntryState = 0;
        public void Update(NwPlayer player, Quest quest, StageNodeWrapper stage)
        {
            var time = NWN.Core.NWNX.UtilPlugin.GetWorldTime();
            var entry = NWN.Core.NWNX.PlayerPlugin.GetJournalEntry(player.ControlledCreature!.ObjectId, quest.Tag);

            var objStr = stage.GetObjectivesJournalEntry(player);
            if(!string.IsNullOrEmpty(objStr))
                objStr = ObjectiveSeparatorString + objStr;

            var text = _stringBuilder.ToString() + objStr;

            if (string.IsNullOrEmpty(entry.sTag))
            {
                entry = new()
                {
                    sTag = quest.Tag,
                    sName = quest.Name,
                    nUpdated = 1,
                    nQuestDisplayed = 1
                };
            }

            entry.nCalendarDay = time.nCalendarDay;
            entry.nTimeOfDay = time.nTimeOfDay;
            entry.nState = nEntryState;
            entry.sText = text;

            nEntryState = NWN.Core.NWNX.PlayerPlugin.AddCustomJournalEntry(player.ControlledCreature!.ObjectId, entry, SilentUpdate ? 1 : 0);

            SilentUpdate = false;
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
            entry.Text = _stringBuilder.ToString();
            entry.Priority = 0;
            entry.State = (uint)nEntryState;
            entry.Updated = true;
            entry.QuestCompleted = true;
            entry.QuestDisplayed = true;

            nEntryState = player.AddCustomJournalEntry(entry);

            _stringBuilder.Clear();
        }
    }
}