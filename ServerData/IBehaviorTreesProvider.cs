using Anvil.API;

namespace ServerData
{
    public interface IBehaviorTreesProvider
    {
        public object? GetBehaviorTreeRootForCreature(NwCreature? creature);
    }
}