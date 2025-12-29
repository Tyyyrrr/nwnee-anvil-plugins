using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Anvil.API;
using NLog;

namespace QuestSystem.Objectives
{
    [JsonPolymorphic(
        IgnoreUnrecognizedTypeDiscriminators = false, 
        TypeDiscriminatorPropertyName = "$objectiveType", 
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(ObjectiveDeliver),"$deliver")]
    [JsonDerivedType(typeof(ObjectiveExplore),"$explore")]
    [JsonDerivedType(typeof(ObjectiveInteract),"$interact")]
    [JsonDerivedType(typeof(ObjectiveKill),"$kill")]
    [JsonDerivedType(typeof(ObjectiveObtain),"$obtain")]
    [JsonDerivedType(typeof(ObjectiveSpellcast),"$spellcast")]
    public abstract partial class Objective
    {
        internal QuestStage? QuestStage;

        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public string JournalEntry {get;set;} = string.Empty;
        public int NextStageID {get;set;} = -1;
        public bool PartyMembersAllowed {get;set;} = false;

        private readonly Dictionary<NwPlayer, IObjectiveProgress> _trackedProgress = new();

        protected internal NwPlayer? GetTrackedPlayer(IObjectiveProgress progress) => _trackedProgress.FirstOrDefault(kvp=>kvp.Value == progress).Key;
        protected internal IObjectiveProgress? GetTrackedProgress(NwPlayer player) => _trackedProgress.TryGetValue(player, out var progress) ? progress : null;
        
        protected internal abstract void Subscribe();
        protected internal abstract void Unsubscribe();

        internal void StartTrackingProgress(NwPlayer player)
        {
            bool shouldSubscribe = !IsActive;

            if (!player.IsValid)
            {
                StopTrackingProgress(player);
                return;
            }

            _log.Warn($"Track progress for player {player.PlayerName}");

            if (IsTracking(player))
            {
                _log.Error("Player is already tracked!");
                return;
            }

            if(shouldSubscribe){
                _log.Warn("Subscribing...");
                Subscribe();
            }

            var progress = CreateProgress();
            progress.OnUpdate += OnProgressUpdate;
            _trackedProgress.Add(player, progress);
        }

        internal void StopTrackingProgress()
        {
            _log.Warn("Stop tracking progress for all players");
            foreach(var kvp in _trackedProgress)
            {
                kvp.Value.OnUpdate -= OnProgressUpdate;
            }
            _trackedProgress.Clear();
            
            _log.Warn("Unsubscribing...");
            Unsubscribe();
        }
        internal void StopTrackingProgress(NwPlayer player)
        {
            _log.Warn($"Stop tracking progress for player {(player.IsValid ? player.PlayerName : "INVALID PLAYER")}");
            if(_trackedProgress.TryGetValue(player, out var progress))
            {
                progress.OnUpdate -= OnProgressUpdate;
                _ = _trackedProgress.Remove(player);
            }

            if(!IsActive)
            {
                _log.Warn("Unsubscribing...");
                Unsubscribe();
            }
        }

        internal bool IsTracking(NwPlayer player) => _trackedProgress.ContainsKey(player);
        internal bool IsActive => _trackedProgress.Count > 0;
        internal bool IsCompleted(NwPlayer player) => _trackedProgress.TryGetValue(player, out var progress) && progress.IsCompleted(this);
        
        internal virtual string GetJournalText(NwPlayer player)
        {
            if(IsCompleted(player)) 
                return $"{JournalEntry} (Ukończono)";
            else return JournalEntry;
        }

        internal abstract IObjectiveProgress CreateProgress();

        private void OnProgressUpdate(IObjectiveProgress progress)
        {
            _log.Info(" - - on progress update --");
            var player = GetTrackedPlayer(progress) ?? throw new InvalidOperationException("Progress object is not owned by this objective.");

            if (!player.IsValid)
            {
                Quest.ClearPlayer(player);
                return;
            }

            var stage = QuestStage ?? throw new InvalidOperationException("No parent QuestStage");
            
            stage.ScheduleJournalUpdate(player);
        }
    }
}