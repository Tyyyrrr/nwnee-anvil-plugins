using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Anvil.API;

using QuestSystem.Nodes;
using QuestSystem.Wrappers.Objectives;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class StageNodeWrapper : NodeWrapper<StageNode>
    {
        public override bool IsRoot => true;

        public bool ShowInJournal => Node.ShowInJournal;

        public string? GetStageJournalEntry(NwPlayer player)
        {
            if(!ShowInJournal) return string.Empty;

            var gender = player.ControlledCreature!.Gender;
            var text = Node.JournalEntry;

            return Regex.Replace(text, "<([^>/]+)/([^>]+)>", m =>
            {
                // text with token example: "Hello <boy/girl>!"
                string left  = m.Groups[1].Value;   // text before '/'
                string right = m.Groups[2].Value;   // text after '/'

                bool condition = gender == Gender.Male;

                return condition ? left : right;
            });
        }
        
        public string? GetObjectivesJournalEntry(NwPlayer player)
        {
            if(!ShowInJournal || _objectives.Length == 0) return string.Empty;

            string text = string.Empty;

            var gender = player.ControlledCreature!.Gender;
            
            foreach(var obj in _objectives.Where(o=>o.ShowInJournal))
            {
                var t = $"{obj.GetJournalText(player)}\n";
                text += Regex.Replace(t, "<([^>/]+)/([^>]+)>", m =>
                {
                    // text with token example: "Hello <boy/girl>!"
                    string left  = m.Groups[1].Value;   // text before '/'
                    string right = m.Groups[2].Value;   // text after '/'

                    bool condition = gender == Gender.Male;

                    return condition ? left : right;
                });
            }

            return text;
        }

        private readonly ObjectiveWrapper[] _objectives;

        public IReadOnlyList<ObjectiveWrapper> Objectives => _objectives;

        public StageNodeWrapper(StageNode questStage) : base(questStage)
        {
            _objectives = new ObjectiveWrapper[questStage.Objectives.Length];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = (ObjectiveWrapper)questStage.Objectives[i].Wrap();
                _objectives[i].Updated += OnObjectiveUpdated;
            }
        }

        bool updateLazy = false;
        void OnObjectiveUpdated(ObjectiveWrapper wrapper, NwPlayer player) {
            
            if(updateLazy)
            {
                _ = NwTask.Run(async () =>
                {
                    try{
                    await NwTask.Delay(TimeSpan.FromSeconds(1.5));
                    await NwTask.SwitchToMainThread();
                    if(updateLazy == true) return;
                    OnObjectiveUpdated(wrapper,player);
                    }
                    catch(Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger()
                            .Error("Exception from async task: " + ex.Message + "\n" + ex.StackTrace);
                    }

                });
                return;
            }

            var data = QuestManager.GetPlayerQuestData(player, Quest);

            if(this._objectives.Any(o=>!o.IsCompleted(player)))
            {
                data?.SilentNextJournalUpdate();
                data?.Update();
            }
            else RaiseShouldEvaluate(player);
        }

        public override void Enter(NwPlayer player)
        {
            updateLazy = true;
            foreach(var objective in _objectives)
                objective.StartTrackingProgress(player);
            updateLazy = false;
            var data = QuestManager.GetPlayerQuestData(player,Quest);
            data?.PushStage(this);
        }

        public override void Reset(NwPlayer player)
        {
            foreach(var objective in _objectives)
                objective.StopTrackingProgress(player);

            var data = QuestManager.GetPlayerQuestData(player,Quest);
            data?.Update();
        }

        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            bool allCompleted = true;
            foreach(var objective in _objectives)
            {
                if(objective.NextID != -1)
                {
                    if(!objective.IsCompleted(player))
                        continue;

                    nextId = objective.NextID;
                    return true;
                }
                else if (!objective.IsCompleted(player))
                {
                    allCompleted = false;
                }
            }
            nextId = Node.NextID;
            return allCompleted;
        }        
        
        protected override void ProtectedDispose()
        {
            foreach(var objective in _objectives)
                objective.Dispose();
        }

        public void JournalComplete(NwPlayer player)
        {
            var data = QuestManager.GetPlayerQuestData(player, Quest) ?? throw new InvalidOperationException("No player data");

            data.CompleteAtStage(this);
        }
    }
}