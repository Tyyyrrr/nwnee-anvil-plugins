using System.Collections.Generic;
using Anvil.API;

namespace BehaviorTrees.Core
{
    /// <summary>
    /// Keeps track of creatures, about which this creature is aware of.<br/>Stores reference to the creature itself too,<br/>and also carries an arbitrary object to use during tree evaluation to pass the data between nodes.
    /// </summary>
    public interface IBehaviorState
    {
        /// <summary>
        /// Creature using this behavior.
        /// </summary>
        public NwCreature Creature {get;}

        /// <summary>
        /// Creatures of which this creature is aware.
        /// </summary>
        public IReadOnlyList<NwCreature> PerceivedCreatures {get;}
        
        /// <summary>
        /// Arbitrary object passed to child nodes during evaluation. It can be a creature, placeable, item, or a custom container for state data.
        /// </summary>
        public object? Context {get;set;}
    }
}