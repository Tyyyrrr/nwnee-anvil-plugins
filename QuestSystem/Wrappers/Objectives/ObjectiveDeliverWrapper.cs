using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveDeliverWrapper : ObjectiveWrapper<ObjectiveDeliver>
    {
        public ObjectiveDeliverWrapper(ObjectiveDeliver objective) : base(objective) { }
        protected override ObjectiveDeliver Objective => base.Objective;

        private readonly List<NwTrigger> _subscribedTriggers = new();

        protected override void Subscribe()
        {
            if(string.IsNullOrEmpty(Objective.ItemResRef) && string.IsNullOrEmpty(Objective.ItemTag))
            {
                _log.Error("ObjectiveObtain needs ResRef, Tag or both, but none was provided");
                return;
            }
            
            _subscribedTriggers.Clear();
            
            foreach(var area in NwModule.Instance.Areas)
            {
                foreach(var trigger in area.FindObjectsOfTypeInArea<NwTrigger>())
                {
                    if(Objective.TriggerTags.Contains(trigger.Tag))
                    {
                        trigger.OnEnter += OnTriggerEnter;
                        _subscribedTriggers.Add(trigger);
                    }
                }
            }
        }

        protected override void Unsubscribe()
        {
            foreach(var trigger in _subscribedTriggers)
                trigger.OnEnter -= OnTriggerEnter;
        }

        void OnTriggerEnter(TriggerEvents.OnEnter data)
        {
            var creature = data.EnteringObject as NwCreature;
            if(creature == null || !creature.IsValid) return;

            var player = creature.ControllingPlayer;
            if(player == null || !player.IsValid) return;

            UpdatePlayer(player);
        }

        void UpdatePlayer(NwPlayer player)
        {
            var progress = GetTrackedProgress(player);
            if(progress == null) return;

            var creature = player.ControlledCreature!;

            bool checkResRef = !string.IsNullOrEmpty(Objective.ItemResRef);
            bool checkTag = !string.IsNullOrEmpty(Objective.ItemTag);

            List<NwItem> itemsToDeliver = new();
            int collectedAmount = 0;

            foreach(var item in creature.Inventory.Items)
            {
                if(!item.IsValid) continue;
                if(checkResRef && item.ResRef != Objective.ItemResRef) continue;
                if(checkTag && item.Tag != Objective.ItemTag) continue;

                collectedAmount += item.StackSize;
                itemsToDeliver.Add(item);
            }

            if(!Objective.AllowPartialDelivery && collectedAmount < Objective.RequiredAmount) return;

            if(Objective.AllowPartialDelivery)
            {
                int current = (int)progress.GetProgressValue()!;
                int target = Objective.RequiredAmount;

                while(itemsToDeliver.Count > 0)
                {
                    var remaining = target - current;
                    var item = itemsToDeliver[0];
                    if(item.StackSize > remaining)
                    {
                        item.StackSize -= remaining;
                        progress.Proceed(target);
                        return;
                    }
                    else
                    {
                        current += item.StackSize;
                        itemsToDeliver.Remove(item);
                        item.Destroy();
                    }
                    
                    if(current >= target)
                    {
                        progress.Proceed(target);
                        return;
                    }
                }
            }
            else if(Objective.DestroyItemsOnDelivery)
            {
                int idx = 0;
                while(collectedAmount > 0)
                {
                    var item = itemsToDeliver[idx];
                    if(item.StackSize > collectedAmount)
                    {
                        item.StackSize -= collectedAmount;
                        break;
                    }
                    else if(item.StackSize == collectedAmount)
                    {
                        item.Destroy();
                        break;
                    }
                    else
                    {
                        collectedAmount -= item.StackSize;
                        item.Destroy();
                        idx++;
                    }
                }
                progress.Proceed(Objective.RequiredAmount);
            }
            else
            {
                progress.Proceed(Objective.RequiredAmount);
            }
        }

        public override void StartTrackingProgress(NwPlayer player)
        {
            base.StartTrackingProgress(player);

            UpdatePlayer(player);
        }
    }
}