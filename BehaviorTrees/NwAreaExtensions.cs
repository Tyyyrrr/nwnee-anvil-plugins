using System.Linq;
using Anvil.API;

namespace BehaviorTrees
{
    internal static class NwAreaExtensions
    {
        public static void EvaluateBehaviorTrees(this NwArea area)
        {
            foreach(NwCreature creature in area.Objects.Where(c=>c is NwCreature).Cast<NwCreature>())
                creature.EvaluateBehaviorTree();
            
        }
    }
}