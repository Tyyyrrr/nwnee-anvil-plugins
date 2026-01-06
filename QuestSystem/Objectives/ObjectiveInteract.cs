using System;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveInteract : Objective
    {
        public enum InteractionType
        {
            PlaceableUse,
            ItemActivate,
            TriggerEnter,
            ObjectExamine
        }

        public InteractionType Interaction {get;set;}

        public string ResRef {get;set;} = string.Empty;
        public string Tag {get;set;} = string.Empty;

        private sealed class Progress : IObjectiveProgress
        {
            private bool _interacted = false;
            public event Action<IObjectiveProgress>? OnUpdate;

            public bool IsCompleted(Objective _) => _interacted;

            public void Proceed(object? _ = null)
            {
                if(_interacted) return;
                _interacted = true;
                OnUpdate?.Invoke(this);
            }
        }

        internal override IObjectiveProgress CreateProgress() => new Progress();

        protected internal override void Subscribe()
        {
            switch (Interaction)
            {
                case InteractionType.PlaceableUse:
                    SubscribePlaceableUse();
                    break;
                case InteractionType.ItemActivate:
                    SubscribeItemActivate();
                    break;
                case InteractionType.TriggerEnter:
                    SubscribeTriggerEnter();
                    break;
                case InteractionType.ObjectExamine:
                    SubscribeObjectExamine();
                    break;
            }
        }

        protected internal override void Unsubscribe()
        {
            switch (Interaction)
            {
                case InteractionType.PlaceableUse:
                    UnsubscribePlaceableUse();
                    break;
                case InteractionType.ItemActivate:
                    UnsubscribeItemActivate();
                    break;
                case InteractionType.TriggerEnter:
                    UnsubscribeTriggerEnter();
                    break;
                case InteractionType.ObjectExamine:
                    UnsubscribeObjectExamine();
                    break;
            }
        }

        private void SubscribePlaceableUse()
        {
            foreach(var area in NwModule.Instance.Areas)
            {
                if(AreaTags.Length > 0 && !AreaTags.Contains(area.Tag)) continue;

                foreach(var obj in area.Objects)
                {
                    if(obj is not NwPlaceable placeable) continue;

                    if((Tag == string.Empty && ResRef != string.Empty && ResRef == placeable.ResRef)
                        || (ResRef == string.Empty && Tag != string.Empty && Tag == placeable.Tag)
                        || (ResRef != string.Empty && Tag != string.Empty && ResRef == placeable.ResRef && Tag == placeable.Tag))
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
            foreach(var area in NwModule.Instance.Areas)
            {
                if(AreaTags.Length > 0 && !AreaTags.Contains(area.Tag)) continue;

                foreach(var obj in area.Objects)
                {
                    if(obj is not NwPlaceable placeable) continue;
                    
                    if((Tag == string.Empty && ResRef != string.Empty && ResRef == placeable.ResRef)
                        || (ResRef == string.Empty && Tag != string.Empty && Tag == placeable.Tag)
                        || (ResRef != string.Empty && Tag != string.Empty && ResRef == placeable.ResRef && Tag == placeable.Tag))
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

            if(usingPlayer == null)
            {
                var master = user.Master;
                usingPlayer = master?.ControllingPlayer ?? null;
            }

            if(usingPlayer == null) return;

            if(PartyMembersAllowed)
            {
                foreach(var member in usingPlayer.PartyMembers)
                {
                    var controlledCreature = member.ControlledCreature;
                    if(controlledCreature == null || userArea != controlledCreature.Area) continue;
                    (GetTrackedProgress(member) as Progress)?.Proceed();
                }
            }
            else (GetTrackedProgress(usingPlayer) as Progress)?.Proceed();
        }

        void OnPlaceableEnterArea(AreaEvents.OnEnter data)
        {
            if(data.EnteringObject is not NwPlaceable placeable) return;

            if((Tag == string.Empty && ResRef != string.Empty && ResRef == placeable.ResRef)
                || (ResRef == string.Empty && Tag != string.Empty && Tag == placeable.Tag)
                || (ResRef != string.Empty && Tag != string.Empty && ResRef == placeable.ResRef && Tag == placeable.Tag))
            {
                placeable.OnUsed -= OnPlaceableUsed;
                placeable.OnUsed += OnPlaceableUsed;
            }
        }        
        
        void OnPlaceableExitArea(AreaEvents.OnExit data)
        {
            if(data.ExitingObject is not NwPlaceable placeable) return;

            if((Tag == string.Empty && ResRef != string.Empty && ResRef == placeable.ResRef)
                || (ResRef == string.Empty && Tag != string.Empty && Tag == placeable.Tag)
                || (ResRef != string.Empty && Tag != string.Empty && ResRef == placeable.ResRef && Tag == placeable.Tag))
            {
                placeable.OnUsed -= OnPlaceableUsed;
            }
        }
        
        private void SubscribeTriggerEnter() => NwModule.Instance.OnTriggerEnter += OnTriggerEntered;
        private void UnsubscribeTriggerEnter() => NwModule.Instance.OnTriggerEnter -= OnTriggerEntered;

        void OnTriggerEntered(OnTriggerEnter data)
        {            
            var trigger = data.Trigger;
            var triggerArea = trigger.Area;
            var enteringCreature = data.EnteredObject as NwCreature;

            if(enteringCreature == null || !enteringCreature.IsPlayerControlled(out var enteringPlayer)) return;

            if(AreaTags.Length > 0 && (triggerArea == null || !AreaTags.Contains(triggerArea.Tag)))
                return;           

            if((Tag == string.Empty && ResRef != string.Empty && ResRef == trigger.ResRef)
                || (ResRef == string.Empty && Tag != string.Empty && Tag == trigger.Tag)
                || (ResRef != string.Empty && Tag != string.Empty && ResRef == trigger.ResRef && Tag == trigger.Tag))
            {
                if (PartyMembersAllowed)
                {
                    foreach(var player in enteringPlayer.PartyMembers)
                    {
                        var controlledCreature = player.ControlledCreature;
                        if(controlledCreature == null || controlledCreature.Area != triggerArea) continue;
                        (GetTrackedProgress(player) as Progress)?.Proceed();
                    }
                }
                else
                {
                    (GetTrackedProgress(enteringPlayer) as Progress)?.Proceed();
                }
            }
        }


        private void SubscribeItemActivate() => NwModule.Instance.OnActivateItem += OnItemActivated;
        private void UnsubscribeItemActivate() => NwModule.Instance.OnActivateItem -= OnItemActivated;

        void OnItemActivated(ModuleEvents.OnActivateItem data)
        {
            
        }

        private void SubscribeObjectExamine() => NwModule.Instance.OnExamineObject += OnObjectExamined;
        private void UnsubscribeObjectExamine() => NwModule.Instance.OnExamineObject -= OnObjectExamined;

        void OnObjectExamined(OnExamineObject data)
        {
            var obj = data.ExaminedObject;

            var area = obj.Area;

            if(AreaTags.Length > 0 && (area == null || !AreaTags.Contains(area.Tag)))
                return;

            var player = data.ExaminedBy;

            if((Tag == string.Empty && ResRef != string.Empty && ResRef == obj.ResRef)
                || (ResRef == string.Empty && Tag != string.Empty && Tag == obj.Tag)
                || (ResRef != string.Empty && Tag != string.Empty && ResRef == obj.ResRef && Tag == obj.Tag))
            {
                if (PartyMembersAllowed)
                {
                    foreach(var member in player.PartyMembers)
                    {
                        var controlledCreature = player.ControlledCreature;
                        if(controlledCreature == null || controlledCreature.Area != area) continue;
                        (GetTrackedProgress(member) as Progress)?.Proceed();
                    }
                }
                else
                {
                    (GetTrackedProgress(player) as Progress)?.Proceed();
                }
            }
        }
    }
}