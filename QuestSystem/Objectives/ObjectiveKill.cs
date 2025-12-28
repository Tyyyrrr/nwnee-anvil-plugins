using System;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;

namespace QuestSystem.Objectives
{
    public sealed class ObjectiveKill : Objective
    {
        public string ResRef {get;set;} = string.Empty;
        public string Tag {get;set;} = string.Empty;
        public string[] AreaTags {get;set;} = Array.Empty<string>();
        public int Amount {get;set;}

        private sealed class Progress : IObjectiveProgress
        {
            private int _kills = 0;
            public event Action<IObjectiveProgress>? OnUpdate;
            public bool IsCompleted(Objective objective) => _kills >= ((ObjectiveKill)objective).Amount;
            public void Proceed(object? _ = null)
            {
                _kills++;
                OnUpdate?.Invoke(this);
            }
        }
        internal override IObjectiveProgress CreateProgress() => new Progress();


        protected internal override void Subscribe()
        {
            foreach(var area in NwModule.Instance.Areas)
            {
                if(AreaTags.Length > 0 && !AreaTags.Contains(area.Tag)) continue;

                foreach(var obj in area.Objects)
                {
                    if(obj is not NwCreature creature) continue;

                    if((Tag == string.Empty && ResRef != string.Empty && ResRef == creature.ResRef)
                        || (ResRef == string.Empty && Tag != string.Empty && Tag == creature.Tag)
                        || (ResRef != string.Empty && Tag != string.Empty && ResRef == creature.ResRef && Tag == creature.Tag))
                    {
                        creature.OnDeath += OnCreatureDeath;
                    }
                }

                area.OnEnter += OnAreaEnter;

                if(AreaTags.Length > 0) area.OnExit += OnAreaExit;
            }
        }

        protected internal override void Unsubscribe()
        {
            foreach(var area in NwModule.Instance.Areas)
            {
                if(AreaTags.Length > 0 && !AreaTags.Contains(area.Tag)) continue;

                foreach(var obj in area.Objects)
                {
                    if(obj is not NwCreature creature) continue;

                    if(ResRef == creature.ResRef || (Tag != string.Empty && Tag == creature.Tag))
                    creature.OnDeath -= OnCreatureDeath;
                }

                area.OnEnter -= OnAreaEnter;
                area.OnExit -= OnAreaExit;
            }
        }

        void OnCreatureDeath(CreatureEvents.OnDeath data)
        {
            var victim = data.KilledCreature;
            var killerObj = data.Killer;

            NwPlayer? killerPlayer = null;
            NwArea? killerArea = null;

            if(killerObj is NwTrappable trappable)
            {
                killerPlayer = trappable.TrapCreator;
                killerArea = trappable.Area;
            }
            else if(killerObj is NwTrigger trigger)
            {
                killerPlayer = trigger.TrapCreator;
                killerArea = trigger.Area;
            }
            else if(killerObj is NwCreature creature)
            {
                killerArea = creature.Area;
                if(!creature.IsPlayerControlled(out killerPlayer) && creature.Master != null)
                {
                    killerPlayer = creature.ControllingPlayer;
                }
            }

            if(killerPlayer == null) return;

            if (PartyMembersAllowed)
            {
                foreach(var player in killerPlayer.PartyMembers)
                {
                    var controlledCreature = player.ControlledCreature;
                    if(controlledCreature == null || killerArea != controlledCreature.Area) continue;
                    (GetTrackedProgress(player) as Progress)?.Proceed();
                }
            }
            else
            {
                (GetTrackedProgress(killerPlayer) as Progress)?.Proceed();
            }


            victim.OnDeath -= OnCreatureDeath;
        }

        void OnAreaEnter(AreaEvents.OnEnter data)
        {
            var creature = data.EnteringObject as NwCreature;

            if(creature == null) return;

            if((Tag == string.Empty && ResRef != string.Empty && ResRef == creature.ResRef)
                || (ResRef == string.Empty && Tag != string.Empty && Tag == creature.Tag)
                || (ResRef != string.Empty && Tag != string.Empty && ResRef == creature.ResRef && Tag == creature.Tag))
            {
                creature.OnDeath -= OnCreatureDeath;
                creature.OnDeath += OnCreatureDeath;
            }
        }

        void OnAreaExit(AreaEvents.OnExit data)
        {
            var creature = data.ExitingObject as NwCreature;

            if(creature == null) return;

            if((Tag == string.Empty && ResRef != string.Empty && ResRef == creature.ResRef)
                || (ResRef == string.Empty && Tag != string.Empty && Tag == creature.Tag)
                || (ResRef != string.Empty && Tag != string.Empty && ResRef == creature.ResRef && Tag == creature.Tag))
            {
                creature.OnDeath -= OnCreatureDeath;
            }
        }
    }
}