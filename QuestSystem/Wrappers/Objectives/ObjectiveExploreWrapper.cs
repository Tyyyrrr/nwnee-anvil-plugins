using System;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using QuestSystem.Objectives;

namespace QuestSystem.Wrappers.Objectives
{
    internal sealed class ObjectiveExploreWrapper : ObjectiveWrapper<ObjectiveExplore>
    {
        public ObjectiveExploreWrapper(ObjectiveExplore objective) : base(objective) { }

        protected override ObjectiveExplore Objective => base.Objective;

        protected override void Subscribe()
        {
            if(Objective.AreaTags.Length == 0)
            {
                _log.Error("ObjectiveExplore needs at least one area tag, but none was provided");
                return;
            }
            

            foreach(var area in NwModule.Instance.Areas)
            {
                if(!Objective.AreaTags.Contains(area.Tag)) continue;

                area.OnHeartbeat += OnAreaHeartbeat;
            }
        }

        protected override void Unsubscribe()
        {
            foreach(var area in NwModule.Instance.Areas)
                area.OnHeartbeat -= OnAreaHeartbeat;
        }

        void OnAreaHeartbeat(AreaEvents.OnHeartbeat data)
        {
            var area = data.Area;

            foreach(var obj in area.Objects)
            {
                if(obj is not NwCreature creature) continue;

                var player = creature.ControllingPlayer;

                if(player == null || !player.IsValid) continue;

                var progress = GetTrackedProgress(player);

                if(progress == null) continue;

                int percentage = RecalculatePercentage(area,player);

                progress.Proceed(percentage);
            }
        }

        static int RecalculatePercentage(NwArea area, NwPlayer player)
        {
            var explorationState = player.GetAreaExplorationState(area);

            if(explorationState == null) return 0;

            return (int)Math.Ceiling(((float)explorationState.Count(b=>b > 0)) / explorationState.Length);
        }
    }
}