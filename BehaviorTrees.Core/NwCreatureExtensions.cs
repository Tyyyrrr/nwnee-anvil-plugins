using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Anvil.API;

namespace BehaviorTrees.Core
{
    public static class NwCreatureExtensions
    {
        private static readonly Dictionary<NwCreature, IBehaviorState> _behaviorStates = new();

        public static bool TryGetBehaviorState(this NwCreature creature, [NotNullWhen(true)] out IBehaviorState? state) => _behaviorStates.TryGetValue(creature, out state) && creature.IsValid;
        public static IBehaviorState? GetBehaviorState(this NwCreature creature)
        {
            if(TryGetBehaviorState(creature, out var state))
                return state;
                
            return null;
        }

        public static bool TryGetBehaviorState<TState>(this NwCreature creature, [NotNullWhen(true)] out TState? state) where TState : class, IBehaviorState
        {
            if(TryGetBehaviorState(creature, out var ibs) && ibs is TState s)
            {
                state = s;
                return true;
            }

            state = null;
            return false;
        }

        public static TState? GetBehaviorState<TState>(this NwCreature creature) where TState : class, IBehaviorState
        {
            if(TryGetBehaviorState<TState>(creature, out var state))
                return state;

            return null;
        }

        public static bool RegisterBehaviorState(this NwCreature creature, IBehaviorState state) =>creature.IsValid && _behaviorStates.TryAdd(creature, state);
        public static bool UnregisterBehaviorState(this NwCreature creature) => _behaviorStates.Remove(creature);
    }
}