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

        private readonly List<NwCreature> _perceivedCreatures = new();
        private readonly List<NwObject> _perceivedObjects = new();
        public IReadOnlyList<NwCreature> PerceivedCreatures => _perceivedCreatures;
        public IReadOnlyList<NwObject> PerceivedObjects => _perceivedObjects;

        internal void AddCreature(NwCreature creature)
        {
            if(_perceivedCreatures.Contains(creature)) return;
                _perceivedCreatures.Add(creature);
        }
        
        internal void AddObject(NwObject obj)
        {
            if(_perceivedObjects.Contains(obj)) return;
                _perceivedObjects.Add(obj);
        }

        internal void RemoveCreature(NwCreature creature) => _perceivedCreatures.Remove(creature);
        internal void RemoveObject(NwObject obj) => _perceivedObjects.Remove(obj);

        internal void ClearInvalidPerceivedCreatures()
        {
            _perceivedCreatures.RemoveAll(c=>!c.IsValid);
        }

        internal void ClearInvalidPerceivedObjects()
        {
            _perceivedObjects.RemoveAll(o=>!o.IsValid);
        }

        public object? Context { get; set; } = null;
    }
}