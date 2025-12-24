using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;
using ExtensionsPlugin;
using NLog;

namespace CharacterAppearance
{
    /// <summary>
    /// Precomputes and caches all head IDs organized by appearance type and gender to avoid runtime evalutaion
    /// </summary>
    internal static class AvailableHeads
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static FrozenDictionary<int, HeadsForAppearanceTypes>? _cachedIDs;

        private sealed class HeadsForAppearanceTypes
        {
            public sealed class HeadsForGenders
            {
                public readonly int[] BaseIDs;
                public readonly int[] AberrationIDs;

                public HeadsForGenders(int[] baseIDs, int[] aberrationIDs)
                {
                    BaseIDs = baseIDs;
                    AberrationIDs = aberrationIDs;
                }
            }

            public readonly HeadsForGenders MaleHeads;
            public readonly HeadsForGenders FemaleHeads;

            public HeadsForAppearanceTypes(HeadsForGenders maleHeads, HeadsForGenders femaleHeads)
            {
                MaleHeads = maleHeads;
                FemaleHeads = femaleHeads;
            }
        }

        private static Task<FrozenDictionary<int, HeadsForAppearanceTypes>> CacheIDsTask()
        {
            var dict = new Dictionary<int, HeadsForAppearanceTypes>();

            var bap = ServerData.DataProviders.BodyAppearanceProvider;

            var raceIDs = NwRuleset.Races.Where(r=>r.IsPlayerRace).Select(r=>r.Id);

            foreach(var raceID in raceIDs)
            {
                var appTypes = bap.GetAppearanceTypesForRace(raceID);

                if(appTypes.Count == 0) continue;
                
                List<int>[] maleHeads = new List<int>[] { new(), new() };
                List<int>[] femaleHeads = new List<int>[] { new(), new() };

                maleHeads[0] = bap.GetHeadsForRace(raceID, Gender.Male).ToList();
                maleHeads[1] = bap.GetHeadsForAberrationAppearance(appTypes[0], Gender.Male).ToList();

                femaleHeads[0] = bap.GetHeadsForRace(raceID, Gender.Female).ToList();
                femaleHeads[1] = bap.GetHeadsForAberrationAppearance(appTypes[0], Gender.Female).ToList();

                var heads = new HeadsForAppearanceTypes(
                    new(maleHeads[0].ToArray(), maleHeads[1].Except(maleHeads[0]).ToArray()),
                    new(femaleHeads[0].ToArray(), femaleHeads[1].Except(femaleHeads[0]).ToArray())
                );

                foreach (var appType in appTypes)
                {
                    _ = dict.TryAdd(appType, heads);
                }
            }

            return Task.FromResult(dict.ToFrozenDictionary());
        }
        public static IList<int> GetHeadsForCreature(NwCreature creature)
        {
            if(_cachedIDs == null)
            {
                _log.Error("Invalid operation - creature head IDs are not cached yet.");
                return Array.Empty<int>();
            }

            if (!_cachedIDs.TryGetValue(creature.Appearance.RowIndex, out var hForAppTypes))
                return Array.Empty<int>();

            HeadsForAppearanceTypes.HeadsForGenders hForGenders = creature.Gender == Gender.Male ? hForAppTypes.MaleHeads : hForAppTypes.FemaleHeads;

            return creature.IsAberration() ? hForGenders.BaseIDs.Concat(hForGenders.AberrationIDs).ToArray() : hForGenders.BaseIDs;
        }

        public static async void CollectAllCreatureHeadsAsync()
        {
            if(_cachedIDs != null)
            {
                _log.Error("Invalid operation - creature heads IDs are already cached.");
                return;
            }
            
            var st = new Stopwatch();
            st.Start();
            _cachedIDs = await NwTask.Run(CacheIDsTask);
            st.Stop();
            _log.Info($"\nCreature heads collected in {st.Elapsed.TotalMilliseconds}ms");
        }
        
    }
}