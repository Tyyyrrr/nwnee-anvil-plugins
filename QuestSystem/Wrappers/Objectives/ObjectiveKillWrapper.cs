using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveKillWrapper : ObjectiveWrapper<ObjectiveKill>
    {
        public ObjectiveKillWrapper(Objective objective) : base(objective) { }

        public override ObjectiveKill Objective => base.Objective;

        protected override void Subscribe()
        {
            foreach (var area in NwModule.Instance.Areas)
            {
                if (Objective.AreaTags.Length > 0 && !Objective.AreaTags.Contains(area.Tag)) continue;

                foreach (var obj in area.Objects)
                {
                    if (obj is not NwCreature creature) continue;

                    if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == creature.ResRef)
                        || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == creature.Tag)
                        || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == creature.ResRef && Objective.Tag == creature.Tag))
                    {
                        creature.OnDeath += OnCreatureDeath;
                    }
                }

                area.OnEnter += OnAreaEnter;

                if (Objective.AreaTags.Length > 0) area.OnExit += OnAreaExit;
            }
        }

        protected override void Unsubscribe()
        {
            foreach (var area in NwModule.Instance.Areas)
            {
                if (Objective.AreaTags.Length > 0 && !Objective.AreaTags.Contains(area.Tag)) continue;

                foreach (var obj in area.Objects)
                {
                    if (obj is not NwCreature creature) continue;

                    if (Objective.ResRef == creature.ResRef || (Objective.Tag != string.Empty && Objective.Tag == creature.Tag))
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

            if (killerObj is NwTrappable trappable)
            {
                killerPlayer = trappable.TrapCreator;
                killerArea = trappable.Area;
            }
            else if (killerObj is NwTrigger trigger)
            {
                killerPlayer = trigger.TrapCreator;
                killerArea = trigger.Area;
            }
            else if (killerObj is NwCreature creature)
            {
                killerArea = creature.Area;
                if (!creature.IsPlayerControlled(out killerPlayer) && creature.Master != null)
                {
                    killerPlayer = creature.ControllingPlayer;
                }
            }

            if (killerPlayer == null) return;

            if (Objective.PartyMembersAllowed)
            {
                foreach (var player in killerPlayer.PartyMembers)
                {
                    var controlledCreature = player.ControlledCreature;
                    if (controlledCreature == null || killerArea != controlledCreature.Area) continue;
                    (GetTrackedProgress(player) as ObjectiveKill.Progress)?.Proceed();
                }
            }
            else
            {
                (GetTrackedProgress(killerPlayer) as ObjectiveKill.Progress)?.Proceed();
            }


            victim.OnDeath -= OnCreatureDeath;
        }

        void OnAreaEnter(AreaEvents.OnEnter data)
        {
            var creature = data.EnteringObject as NwCreature;

            if (creature == null) return;

            if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == creature.ResRef)
                || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == creature.Tag)
                || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == creature.ResRef && Objective.Tag == creature.Tag))
            {
                creature.OnDeath -= OnCreatureDeath;
                creature.OnDeath += OnCreatureDeath;
            }
        }

        void OnAreaExit(AreaEvents.OnExit data)
        {
            var creature = data.ExitingObject as NwCreature;

            if (creature == null) return;

            if ((Objective.Tag == string.Empty && Objective.ResRef != string.Empty && Objective.ResRef == creature.ResRef)
                || (Objective.ResRef == string.Empty && Objective.Tag != string.Empty && Objective.Tag == creature.Tag)
                || (Objective.ResRef != string.Empty && Objective.Tag != string.Empty && Objective.ResRef == creature.ResRef && Objective.Tag == creature.Tag))
            {
                creature.OnDeath -= OnCreatureDeath;
            }
        }
    }
}