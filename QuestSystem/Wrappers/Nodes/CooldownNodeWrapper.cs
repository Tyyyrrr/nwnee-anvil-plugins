using System;
using System.Collections.Generic;

using Anvil.API;

using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class CooldownNodeWrapper : NodeWrapper<CooldownNode>
    {
        public CooldownNodeWrapper(CooldownNode node) : base(node){}

        public override bool IsRoot => false;

        private readonly Dictionary<NwPlayer, DateTimeOffset> _cdStartTimes = new();

        public override void Enter(NwPlayer player)
        {
            if(_cdStartTimes.TryAdd(player,default))
            {
                // TODO: find a way to obtain cooldown start time by cooldown tag
                _cdStartTimes[player] = DateTimeOffset.UtcNow; // tmp!
            }
        }

        public override void Reset(NwPlayer player) => _cdStartTimes.Remove(player);

        protected override void ProtectedDispose() => _cdStartTimes.Clear();
        
        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            var start = _cdStartTimes[player];
            var elapsed = (DateTimeOffset.UtcNow - start).TotalSeconds;
            bool completed = elapsed >= Node.DurationSeconds;
            nextId = completed? -1 : Node.NextID;
            return completed;
        }
    }
}