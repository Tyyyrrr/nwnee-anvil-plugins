using Anvil.API;
using Anvil.API.Events;
using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveObtainWrapper : ObjectiveWrapper<ObjectiveObtain>
    {
        public ObjectiveObtainWrapper(ObjectiveObtain objective) : base(objective) { }

        protected override ObjectiveObtain Objective => base.Objective;

        protected override void Subscribe()
        {
            if(string.IsNullOrEmpty(Objective.ItemResRef) && string.IsNullOrEmpty(Objective.ItemTag))
            {
                _log.Error("ObjectiveObtain needs ResRef, Tag or both, but none was provided");
                return;
            }

            NwModule.Instance.OnAcquireItem += OnItemAcquired;
            NwModule.Instance.OnUnacquireItem += OnItemUnacquired;
        }

        protected override void Unsubscribe()
        {
            NwModule.Instance.OnAcquireItem -= OnItemAcquired;
            NwModule.Instance.OnUnacquireItem -= OnItemUnacquired;
        }

        void OnItemAcquired(ModuleEvents.OnAcquireItem data)
        {
            var creature = data.AcquiredBy as NwCreature;
            if(creature == null || !creature.IsValid) return;
            ScanInventory(creature);
        }

        void OnItemUnacquired(ModuleEvents.OnUnacquireItem data)
        {
            ScanInventory(data.LostBy);
        }

        void ScanInventory(NwCreature creature)
        {
            var player = creature.ControllingPlayer;
            if(player == null || !player.IsValid) return;

            int amount = 0;

            bool checkResRef = !string.IsNullOrEmpty(Objective.ItemResRef);
            bool checkTag = !string.IsNullOrEmpty(Objective.ItemTag);

            foreach(var item in creature.Inventory.Items)
            {
                if(!item.IsValid) continue;
                if(checkResRef && item.ResRef != Objective.ItemResRef) continue;
                if(checkTag && item.Tag != Objective.ItemTag) continue;

                amount += item.StackSize;
            }

            GetTrackedProgress(player)?.Proceed(amount);
        }

        public override void StartTrackingProgress(NwPlayer player)
        {
            base.StartTrackingProgress(player);

            var creature = player.ControlledCreature;

            if(creature != null) ScanInventory(creature);
        }
    }
}