using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using NLog;

using QuestSystem.Objectives;

namespace QuestSystem.Wrappers
{
    internal abstract partial class ObjectiveWrapper
    {
        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();

        protected readonly Objective _baseObjective;
        public abstract Objective Objective { get; }
        public ObjectiveWrapper(Objective objective)
        {
            _baseObjective = objective;
            Reward = new(objective.Reward);
        }

        public event Action<ObjectiveWrapper, NwPlayer>? Completed;

        public readonly QuestStageRewardWrapper Reward;


        protected internal NwPlayer? GetTrackedPlayer(IObjectiveProgress progress) => _trackedProgress.FirstOrDefault(kvp => kvp.Value == progress).Key;
        protected internal IObjectiveProgress? GetTrackedProgress(NwPlayer player) => _trackedProgress.TryGetValue(player, out var progress) ? progress : null;

        private readonly Dictionary<NwPlayer, IObjectiveProgress> _trackedProgress = new();
        public bool IsTracking(NwPlayer player) => _trackedProgress.ContainsKey(player);
        public bool IsActive => _trackedProgress.Count > 0;
        public bool IsCompleted(NwPlayer player) => _trackedProgress.TryGetValue(player, out var progress) && progress.IsCompleted;

        protected abstract void Subscribe();
        protected abstract void Unsubscribe();


        public void StartTrackingProgress(NwPlayer player)
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

            if (shouldSubscribe)
            {
                _log.Warn("Subscribing...");
                Subscribe();
            }

            var progress = Objective.CreateProgressTrack();
            progress.OnUpdate += OnProgressUpdate;
            _trackedProgress.Add(player, progress);
        }

        public void StopTrackingProgress()
        {
            _log.Warn("Stop tracking progress for all players");
            foreach (var kvp in _trackedProgress)
            {
                kvp.Value.OnUpdate -= OnProgressUpdate;
            }
            _trackedProgress.Clear();

            _log.Warn("Unsubscribing...");
            Unsubscribe();
        }
        public void StopTrackingProgress(NwPlayer player)
        {
            _log.Warn($"Stop tracking progress for player {(player.IsValid ? player.PlayerName : "INVALID PLAYER")}");
            if (_trackedProgress.TryGetValue(player, out var progress))
            {
                progress.OnUpdate -= OnProgressUpdate;
                _ = _trackedProgress.Remove(player);
            }

            if (!IsActive)
            {
                _log.Warn("Unsubscribing...");
                Unsubscribe();
            }
        }


        public virtual string GetJournalText(NwPlayer player)
        {
            if (!Objective.ShowInJournal) return string.Empty;

            if (IsCompleted(player)) return $"{Objective.JournalEntry} (Ukończono)";

            else return Objective.JournalEntry;
        }

        private void OnProgressUpdate(IObjectiveProgress progress)
        {
            _log.Info(" - - on progress update --");

            var player = GetTrackedPlayer(progress) ?? throw new InvalidOperationException("Progress object is not owned by this objective.");

            if(IsCompleted(player)) Completed?.Invoke(this, player);
            
        }
    }

    internal abstract class ObjectiveWrapper<T> : ObjectiveWrapper where T : Objective
    {
        protected ObjectiveWrapper(Objective objective) : base(objective) { }

        public override T Objective => (T)_baseObjective;
    }
}