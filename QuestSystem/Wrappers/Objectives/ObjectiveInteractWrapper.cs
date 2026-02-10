using System;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveInteractWrapper : ObjectiveWrapper<ObjectiveInteract>
    {
        public ObjectiveInteractWrapper(ObjectiveInteract objective) : base(objective) { }

        protected override ObjectiveInteract Objective => base.Objective;


        protected override void Subscribe()
        {
            if(string.IsNullOrEmpty(Objective.ResRef) && string.IsNullOrEmpty(Objective.Tag))
            {
                _log.Error("ObjectiveInteract needs ResRef, Tag or both, but none was provided");
                return;
            }

            switch (Objective.Interaction)
            {
                case ObjectiveInteract.InteractionType.PlaceableUse:
                    SubscribePlaceableUse();
                    break;
                case ObjectiveInteract.InteractionType.ItemActivate:
                    SubscribeItemActivate();
                    break;
                case ObjectiveInteract.InteractionType.TriggerEnter:
                    SubscribeTriggerEnter();
                    break;
                case ObjectiveInteract.InteractionType.ObjectExamine:
                    SubscribeObjectExamine();
                    break;
            }
        }

        protected override void Unsubscribe()
        {
            switch (Objective.Interaction)
            {
                case ObjectiveInteract.InteractionType.PlaceableUse:
                    UnsubscribePlaceableUse();
                    break;
                case ObjectiveInteract.InteractionType.ItemActivate:
                    UnsubscribeItemActivate();
                    break;
                case ObjectiveInteract.InteractionType.TriggerEnter:
                    UnsubscribeTriggerEnter();
                    break;
                case ObjectiveInteract.InteractionType.ObjectExamine:
                    UnsubscribeObjectExamine();
                    break;
            }
        }

        private void SubscribePlaceableUse()
        {

            foreach (var area in NwModule.Instance.Areas)
            {
                if (Objective.AreaTags.Length > 0 && !Objective.AreaTags.Contains(area.Tag)) continue;

                foreach (var obj in area.Objects)
                {
                    if (obj is not NwPlaceable placeable) continue;

                    if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == placeable.ResRef)
                        || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == placeable.Tag)
                        || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == placeable.ResRef && Objective.Tag == placeable.Tag))
                    {
                        placeable.OnUsed += OnPlaceableUsed;
                    }
                }

                area.OnEnter += OnPlaceableEnterArea;
                area.OnExit += OnPlaceableExitArea;
            }
        }


        private void UnsubscribePlaceableUse()
        {
            foreach (var area in NwModule.Instance.Areas)
            {
                if (Objective.AreaTags.Length > 0 && !Objective.AreaTags.Contains(area.Tag)) continue;

                foreach (var obj in area.Objects)
                {
                    if (obj is not NwPlaceable placeable) continue;

                    if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == placeable.ResRef)
                        || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == placeable.Tag)
                        || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == placeable.ResRef && Objective.Tag == placeable.Tag))
                    {
                        placeable.OnUsed -= OnPlaceableUsed;
                    }
                }

                area.OnEnter -= OnPlaceableEnterArea;
                area.OnExit -= OnPlaceableExitArea;
            }
        }

        void OnPlaceableUsed(PlaceableEvents.OnUsed data)
        {
            var user = data.UsedBy;
            var userArea = user.Area;
            var usingPlayer = user.ControllingPlayer;

            if (usingPlayer == null)
            {
                var master = user.Master;
                usingPlayer = master?.ControllingPlayer ?? null;
            }

            if (usingPlayer == null) return;

            
            if (Objective.AreaTags.Length > 0 && (userArea == null || !Objective.AreaTags.Contains(userArea.Tag)))
                return;

            if(Objective.TriggerTags.Length > 0)
            {
                var nearestTriggers = user.GetNearestObjectsByType<NwTrigger>();
                bool inTrigger = false;
                foreach(var t in nearestTriggers)
                {
                    if(!t.IsValid || !Objective.TriggerTags.Contains(t.Tag)) continue;
                    if (t.GetObjectsInTrigger<NwCreature>().Any(c => c == user))
                    {
                        inTrigger = true;
                        break;
                    }
                }

                if(!inTrigger) return;
            }

            if (Objective.PartyMembersAllowed)
            {
                foreach (var member in usingPlayer.PartyMembers)
                {
                    var controlledCreature = member.ControlledCreature;
                    if (controlledCreature == null || userArea != controlledCreature.Area) continue;
                    GetTrackedProgress(member)?.Proceed();
                }
            }
            else GetTrackedProgress(usingPlayer)?.Proceed();
        }

        void OnPlaceableEnterArea(AreaEvents.OnEnter data)
        {
            if (data.EnteringObject is not NwPlaceable placeable) return;

            if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == placeable.ResRef)
                || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == placeable.Tag)
                || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == placeable.ResRef && Objective.Tag == placeable.Tag))
            {
                placeable.OnUsed -= OnPlaceableUsed;
                placeable.OnUsed += OnPlaceableUsed;
            }
        }

        void OnPlaceableExitArea(AreaEvents.OnExit data)
        {
            if (data.ExitingObject is not NwPlaceable placeable) return;

            if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == placeable.ResRef)
                || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == placeable.Tag)
                || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == placeable.ResRef && Objective.Tag == placeable.Tag))
            {
                placeable.OnUsed -= OnPlaceableUsed;
            }
        }

        private void SubscribeTriggerEnter() 
        {
            EventService.SubscribeAll<OnTriggerEnter,OnTriggerEnter.Factory>(OnTriggerEntered, EventCallbackType.After);
        }
        private void UnsubscribeTriggerEnter()
        {
            EventService.UnsubscribeAll<OnTriggerEnter,OnTriggerEnter.Factory>(OnTriggerEntered, EventCallbackType.After);
        }

        void OnTriggerEntered(OnTriggerEnter data)
        {
            var trigger = data.Trigger;
            var triggerArea = trigger.Area;
            var enteringCreature = data.EnteredObject as NwCreature;

            if (enteringCreature == null || !enteringCreature.IsPlayerControlled(out var enteringPlayer)) return;

            if (Objective.AreaTags.Length > 0 && (triggerArea == null || !Objective.AreaTags.Contains(triggerArea.Tag)))
                return;

            if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == trigger.ResRef)
                || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == trigger.Tag)
                || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == trigger.ResRef && Objective.Tag == trigger.Tag))
            {
                if (Objective.PartyMembersAllowed)
                {
                    foreach (var player in enteringPlayer.PartyMembers)
                    {
                        var controlledCreature = player.ControlledCreature;
                        if (controlledCreature == null || controlledCreature.Area != triggerArea) continue;
                        GetTrackedProgress(player)?.Proceed();
                    }
                }
                else
                {
                    GetTrackedProgress(enteringPlayer)?.Proceed();
                }
            }
        }


        private void SubscribeItemActivate() 
        {
            NwModule.Instance.OnActivateItem += OnItemActivated;
        }
        private void UnsubscribeItemActivate() => NwModule.Instance.OnActivateItem -= OnItemActivated;

        void OnItemActivated(ModuleEvents.OnActivateItem data)
        {
            var creature = data.ItemActivator;
            var player = creature.ControllingPlayer;
            if(player == null || !player.IsValid)
            {
                _log.Error("invalid player");
                return;
            }

            var area = creature.Area;
            if(area != null && !area.IsValid){
                _log.Error("item activator not in area");
                return;
            }

            if (Objective.AreaTags.Length > 0 && (area == null || !Objective.AreaTags.Contains(area.Tag)))
                return;

            if(Objective.TriggerTags.Length > 0)
            {
                var nearestTriggers = creature.GetNearestObjectsByType<NwTrigger>();
                bool inTrigger = false;
                foreach(var t in nearestTriggers)
                {
                    if(!t.IsValid || !Objective.TriggerTags.Contains(t.Tag)) continue;
                    if (t.GetObjectsInTrigger<NwCreature>().Any(c => c == creature))
                    {
                        inTrigger = true;
                        break;
                    }
                }

                if(!inTrigger) return;
            }

            var item = data.ActivatedItem;
            if(!item.IsValid) return;
            if(!string.IsNullOrEmpty(Objective.ResRef) && item.ResRef != Objective.ResRef) return;
            if(!string.IsNullOrEmpty(Objective.Tag) && item.Tag != Objective.Tag) return;

            var target = data.TargetObject;
            if(target != null)
            {
                if(!target.IsValid) return;
                if(!string.IsNullOrEmpty(Objective.ItemActivateTargetResRef) && target.ResRef != Objective.ItemActivateTargetResRef) return;
                if(!string.IsNullOrEmpty(Objective.ItemActivateTargetTag) && target.Tag != Objective.ItemActivateTargetTag) return;
            }

            if (Objective.PartyMembersAllowed)
            {
                foreach (var member in player.PartyMembers)
                {
                    var controlledCreature = player.ControlledCreature;

                    if (controlledCreature == null 
                        || !controlledCreature.IsValid 
                        || controlledCreature.Area == null
                        || controlledCreature.Area != area) 
                        continue;

                    GetTrackedProgress(member)?.Proceed();
                }
            }
            else
            {
                GetTrackedProgress(player)?.Proceed();
            }
        }

        private void SubscribeObjectExamine()
        {            
            EventService.SubscribeAll<OnExamineObject,OnExamineObject.Factory>(OnObjectExamined, EventCallbackType.After);
        }
        private void UnsubscribeObjectExamine()
        {
            EventService.UnsubscribeAll<OnExamineObject,OnExamineObject.Factory>(OnObjectExamined, EventCallbackType.After);
        }
     

        void OnObjectExamined(OnExamineObject data)
        {
            var player = data.ExaminedBy;
            var creature = player.ControlledCreature;

            if(player == null || !player.IsValid)
            {
                _log.Error("invalid player");
                return;
            }

            if(creature == null || !creature.IsValid)
            {
                _log.Error("invalid creature");
                return;
            }

            var obj = data.ExaminedObject;

            if(!obj.IsValid){
                _log.Error("examined invalid object");
                return;
            }

            var area = obj.Area;

            if(area != null && !area.IsValid){
                _log.Error("examined object not in area");
                return;
            }

            if (Objective.AreaTags.Length > 0 && (area == null || !Objective.AreaTags.Contains(area.Tag)))
                return;

            if(Objective.TriggerTags.Length > 0)
            {
                var nearestTriggers = creature.GetNearestObjectsByType<NwTrigger>();
                bool inTrigger = false;
                foreach(var t in nearestTriggers)
                {
                    if(!t.IsValid || !Objective.TriggerTags.Contains(t.Tag)) continue;
                    if (t.GetObjectsInTrigger<NwCreature>().Any(c => c == creature))
                    {
                        inTrigger = true;
                        break;
                    }
                }

                if(!inTrigger) return;
            }
                

            if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == obj.ResRef)
                || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == obj.Tag)
                || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == obj.ResRef && Objective.Tag == obj.Tag))
            {
                if (Objective.PartyMembersAllowed)
                {
                    foreach (var member in player.PartyMembers)
                    {
                        var controlledCreature = player.ControlledCreature;

                        if (controlledCreature == null 
                            || !controlledCreature.IsValid 
                            || controlledCreature.Area == null
                            || controlledCreature.Area != area) 
                            continue;

                        GetTrackedProgress(member)?.Proceed();
                    }
                }
                else
                {
                    GetTrackedProgress(player)?.Proceed();
                }
            }
        }
    }
}