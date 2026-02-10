using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.Services;
using NLog;

using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal abstract partial class ObjectiveWrapper : WrapperBase
    {
        private static EventService? _eventService = null;
        public static EventService EventService
        {
            protected get => _eventService ?? throw new InvalidOperationException("Event service instance not provided yet.");
            set
            {
                if(_eventService != null) throw new InvalidProgramException("Event service instance already provided.");
                _eventService = value;
            }
        }

        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();

        protected readonly Objective _baseObjective;
        protected abstract Objective Objective { get; }
        public ObjectiveWrapper(Objective objective)
        {
            _baseObjective = objective;
            _log.Warn($"<{GetType().Name}> Subscribing");
            Subscribe();
        }

        public int NextID => Objective.NextStageID;

        public event Action<ObjectiveWrapper, NwPlayer>? Updated;

        public bool ShowInJournal => _baseObjective.ShowInJournal;

        protected internal NwPlayer? GetTrackedPlayer(IObjectiveProgress progress) => _trackedProgress.FirstOrDefault(kvp => kvp.Value == progress).Key;
        protected internal IObjectiveProgress? GetTrackedProgress(NwPlayer player) => _trackedProgress.TryGetValue(player, out var progress) ? progress : null;

        private readonly Dictionary<NwPlayer, IObjectiveProgress> _trackedProgress = new();
        public bool IsTracking(NwPlayer player) => _trackedProgress.ContainsKey(player);
        public bool IsActive => _trackedProgress.Count > 0;
        public bool IsCompleted(NwPlayer player) => _trackedProgress.TryGetValue(player, out var progress) && progress.IsCompleted;

        protected abstract void Subscribe();
        protected abstract void Unsubscribe();


        public virtual void StartTrackingProgress(NwPlayer player)
        {
            bool shouldSubscribe = !IsActive;

            if (!player.IsValid)
            {
                StopTrackingProgress(player);
                return;
            }

            _log.Warn($" < {GetType().Name} >  Track progress for player {player.PlayerName}");

            if (IsTracking(player))
            {
                _log.Error($" < {GetType().Name} >  Player is already tracked!");
                return;
            }

            var progress = Objective.CreateProgressTrack();
            _trackedProgress.Add(player, progress);
            progress.OnUpdate += OnProgressUpdate;
        }

        public void StopTrackingProgress()
        {
            _log.Warn($" < {GetType().Name} > Stop tracking progress for all players");
            foreach (var kvp in _trackedProgress)
            {
                kvp.Value.OnUpdate -= OnProgressUpdate;
            }
            _trackedProgress.Clear();
        }
        public void StopTrackingProgress(NwPlayer player)
        {
            _log.Warn($" < {GetType().Name} > Stop tracking progress for player {(player.IsValid ? player.PlayerName : "<INVALID>")}");
            if (_trackedProgress.TryGetValue(player, out var progress))
            {
                progress.OnUpdate -= OnProgressUpdate;
                _ = _trackedProgress.Remove(player);
            }
        }


        protected static readonly string ObjectiveJournalPrefix = "   - ";
        public string GetJournalText(NwPlayer player)
        {
            if (!Objective.ShowInJournal) return string.Empty;

            var progress = GetTrackedProgress(player) ?? throw new InvalidOperationException("Progress is not tracked for the player");

            return $"{ObjectiveJournalPrefix}{Objective.JournalEntry} {progress.GetProgressString()}";
        }

        private void OnProgressUpdate(IObjectiveProgress progress)
        {
            _log.Info($" < {GetType().Name} >   - - on progress update --");

            var player = GetTrackedPlayer(progress) ?? throw new InvalidOperationException("Progress object is not owned by this objective.");

            Updated?.Invoke(this, player);
        }

        protected override void ProtectedDispose()
        {
            StopTrackingProgress();

            _log.Warn($"<{GetType().Name}> Unsubscribing");
            
            Unsubscribe();
        }
    }

    internal abstract class ObjectiveWrapper<T> : ObjectiveWrapper where T : Objective
    {
        protected ObjectiveWrapper(Objective objective) : base(objective) { }

        protected override T Objective => (T)_baseObjective;
    }
}