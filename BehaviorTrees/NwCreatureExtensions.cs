using Anvil.API;
using BehaviorTrees.Core;
using BehaviorTrees.Core.Nodes;

namespace BehaviorTrees
{
    internal static class NwCreatureExtensions
    {
        public static void NoticeCreature(this NwCreature creature, NwCreature other)
        {
            if(creature.TryGetBehaviorState<BehaviorState>(out var bs))
                bs.AddCreature(other);
        }

        public static void NoticeObject(this NwCreature creature, NwObject other)
        {
            if(creature.TryGetBehaviorState<BehaviorState>(out var bs))
                bs.AddObject(other);
        }

        public static void UnNoticeCreature(this NwCreature creature, NwCreature other)
        {
            if(creature.TryGetBehaviorState<BehaviorState>(out var bs))
                bs.RemoveCreature(other);
        }
        
        public static void UnNoticeObject(this NwCreature creature, NwObject other)
        {
            if(creature.TryGetBehaviorState<BehaviorState>(out var bs))
                bs.RemoveObject(other);
        }

        public static void RegisterBehaviorTree(this NwCreature creature, Node rootNode)
        {
            if(!creature.IsValid || creature.GetBehaviorState() != null) 
                return;

            var bs = new BehaviorState(creature,rootNode);

            creature.RegisterBehaviorState(bs);
        }

        public static void EvaluateBehaviorTree(this NwCreature creature)
        {
            if(creature.TryGetBehaviorState<BehaviorState>(out var bs))
            {
                if (!creature.IsValid)
                    creature.UnregisterBehaviorState();
                
                else
                {
                    bs.ClearInvalidPerceivedCreatures();
                    bs.ClearInvalidPerceivedObjects();
                    bs.TreeRoot.Evaluate(bs);
                }
            }
        }
    }
}