using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using BehaviorTrees.Core.Nodes;

namespace BehaviorTrees
{
    internal static class NwCreatureExtensions
    {
        private static readonly Dictionary<NwCreature, BehaviorState> _behaviorStates = new();

        public static void NoticeCreature(this NwCreature creature, NwCreature other)
        {
            if(_behaviorStates.TryGetValue(creature, out var bs))
                bs.AddCreature(other);
        }

        public static void UnNoticeCreature(this NwCreature creature, NwCreature other)
        {
            if(_behaviorStates.TryGetValue(creature, out var bs))
                bs.RemoveCreature(other);
        }
        public static void RegisterBehaviorTree(this NwCreature creature, Node rootNode)
        {
            if(_behaviorStates.ContainsKey(creature))
                return;

            _behaviorStates.Add(creature, new(creature, rootNode));
        }

        public static void EvaluateBehaviorTree(this NwCreature creature)
        {
            if(_behaviorStates.TryGetValue(creature, out var bs))
            {
                if (!creature.IsValid)
                    _behaviorStates.Remove(creature);
                
                else
                {
                    bs.ClearInvalidPerceivedCreatures();
                    bs.TreeRoot.Evaluate(bs);
                }
            }
        }
    }
}