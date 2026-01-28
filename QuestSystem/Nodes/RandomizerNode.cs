using System;
using System.Collections.Generic;
using System.Linq;

using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Nodes
{
    public sealed class RandomizerNode : NodeBase
    {
        public static readonly Random _prng = Random.Shared;
        
        public Dictionary<int,float> Branches {get;set;} = new();

        internal override RandomizerNodeWrapper Wrap() => new(this);

        internal int WeightedRandom()
        {
            var total = Branches.Values.Sum();

            float roll = _prng.NextSingle() * total;

            float cumulative = 0f;

            foreach (var kvp in Branches.OrderBy(b => b.Key))
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                    return kvp.Key;
            }

            return Branches.OrderBy(b => b.Key).First().Key;
        }
        public override object Clone()
        {
            var branches = Branches.ToDictionary();

            return new RandomizerNode()
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback,

                Branches = branches
            };
        }
    }
}