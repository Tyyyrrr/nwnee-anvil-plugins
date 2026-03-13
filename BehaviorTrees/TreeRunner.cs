using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace BehaviorTrees
{
    internal sealed class TreeRunner : IDisposable
    {
        private const int EvaluationFrequencyMilliseconds = 1000;
        private const int MeasurePerformanceTicks = 10;
        private const int MaxNonCriticalAreasProcessedPerTick = 100;

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


        static readonly List<NwArea> _criticalAreas = new(1000);
        static readonly Queue<NwArea> _nonCriticalAreas = new(2000);

        static readonly Stopwatch _sw = new();
        static int measured = 0;
        static readonly double[] _measurements = MeasurePerformanceTicks > 0 ? new double[MeasurePerformanceTicks] : Array.Empty<double>();

        static void ProcessAreas(NwModule module)
        {
            _sw.Restart();

            // Order areas. Prioritize areas with players. Limit other areas with queue
            _criticalAreas.Clear();

            foreach(var area in module.Areas)
            {
                if(!area.IsValid) continue;
                else if(area.PlayerCount > 0) _criticalAreas.Add(area);
                else if(_nonCriticalAreas.Contains(area)) continue;
                else _nonCriticalAreas.Enqueue(area);
            }

            // Process areas in prepared order.
            foreach(var area in _criticalAreas)
                area.EvaluateBehaviorTrees();

            int nonCriticalAreasProcessed = 0;

            while(nonCriticalAreasProcessed < MaxNonCriticalAreasProcessedPerTick)
            {
                if(!_nonCriticalAreas.TryDequeue(out var area))
                    break;
                    
                if(area.IsValid)
                    area.EvaluateBehaviorTrees();

                nonCriticalAreasProcessed++;
            }
            
            // Report status every N-th loop
            _measurements[measured++] = _sw.Elapsed.TotalMilliseconds;
            if(measured == MeasurePerformanceTicks - 1)
            {
                _log.Info($"Average BehaviorTree tick performance: {_measurements.Average()}ms");
                measured = 0;
                Array.Fill(_measurements,0d);
            }
        }

        public void Dispose()
        {
            scheduledTask?.Dispose();
            scheduledTask = null;
        }
    }
}