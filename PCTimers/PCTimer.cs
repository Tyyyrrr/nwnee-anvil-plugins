using System;
using System.Diagnostics;
using Anvil.API;

namespace PCTimers
{
    internal sealed class PCTimer
    {
        /// <summary>
        /// Change this script name if to your own .nss handler
        /// </summary>
        public const string TickScriptHandler = "pctimers_tick";

        /// <summary>
        /// Time to wait until processing the next tick
        /// </summary>
        public const float CycleDurationMinutes = 6;


        private static readonly TimeSpan _cycleDuration = TimeSpan.FromMinutes(CycleDurationMinutes);
        private static readonly double _cycleSeconds = _cycleDuration.TotalSeconds;

        private readonly Stopwatch _sw;
        private TimeSpan offset = TimeSpan.Zero;

        public PCTimer(NwCreature pc)
        {
            _pc = pc;
            _sw = new();
            _sw.Start();
        }

        private readonly NwCreature _pc;

        public void Reset()
        {
            offset = TimeSpan.Zero;
            _sw.Restart();
        }

        public void Tick()
        {
            var elapsedSeconds = (_sw.Elapsed + offset).TotalSeconds;
            if(elapsedSeconds >= _cycleSeconds && _pc.IsValid)
            {
                offset = TimeSpan.FromSeconds(elapsedSeconds % _cycleSeconds);
                NWN.Core.NWScript.ExecuteScript(TickScriptHandler,_pc.ObjectId);
                _sw.Restart();
            }
        }

    }
}