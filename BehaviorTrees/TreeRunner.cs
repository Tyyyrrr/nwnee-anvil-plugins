using System;
using System.Diagnostics;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace BehaviorTrees
{
    internal sealed class TreeRunner : IDisposable
    {
        private const int EvaluationFrequencyMilliseconds = 1000;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        
        private readonly SchedulerService _scheduler;
        public TreeRunner(SchedulerService scheduler){_scheduler = scheduler;}


        private void Tick()
        {
            var module = NwModule.Instance;

            if(module == null || !module.IsValid)
                return;

            ProcessAreas(module);
        }


        private ScheduledTask? scheduledTask = null;

        public void Start()
        {
            _log.Info("Starting...");

            scheduledTask = _scheduler.ScheduleRepeating(Tick, TimeSpan.FromMilliseconds(EvaluationFrequencyMilliseconds));
        }

        readonly Stopwatch _sw = new();
        void ProcessAreas(NwModule module)
        {
            _log.Info("Behavior tree runner processing all areas...");

            _sw.Restart();
            
            foreach(var area in module.Areas)
            {
                area.EvaluateBehaviorTrees();
            }

            _sw.Stop();

            _log.Info($"... all area behaviors processed in {_sw.Elapsed.TotalSeconds}");
        }

        public void Dispose()
        {
            scheduledTask?.Dispose();
            scheduledTask = null;
        }
    }
}