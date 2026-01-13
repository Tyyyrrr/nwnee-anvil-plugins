using System;
using System.Collections.Generic;
using System.Linq;

namespace QuestSystem.Nodes
{
    public sealed class RandomizerNode : NodeBase
    {
        public static readonly Random _prng = Random.Shared;
        public override int NextID => WeightedRandom(); // todo: explicitly call function in NodeWrapper's Evaluate()
        public Dictionary<int,float> Branches {get;set;} = new();
        private int WeightedRandom()
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
    }
}