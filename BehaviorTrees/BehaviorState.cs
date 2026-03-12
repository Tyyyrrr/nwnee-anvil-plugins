using System.Collections.Generic;
using Anvil.API;
using BehaviorTrees.Core;
using BehaviorTrees.Core.Nodes;

namespace BehaviorTrees
{
    internal sealed class BehaviorState : IBehaviorState
    {
        public readonly Node TreeRoot;
        public BehaviorState(NwCreature creature, Node treeRoot) {Creature = creature; TreeRoot = treeRoot; }
        public NwCreature Creature {get;}

        private readonly List<NwCreature> _perceived = new();
        public IReadOnlyList<NwCreature> PerceivedCreatures => _perceived;

        internal void AddCreature(NwCreature creature)
        {
            if(_perceived.Contains(creature)) return;
            _perceived.Add(creature);
        }

        internal void RemoveCreature(NwCreature creature) => _perceived.Remove(creature);

        public object? Context { get; set; } = null;
    }
}