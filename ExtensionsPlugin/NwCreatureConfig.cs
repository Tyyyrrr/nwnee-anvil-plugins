using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

using Anvil.API;

using EasyConfig;


namespace ExtensionsPlugin
{
    [ConfigFile("NwCreature")]
    public sealed class NwCreatureExtensionsConfig : IConfig
    {
        internal const string DefaultHumanMalePortrait = "hu_m_99_";
        internal const string DefaultHumanFemalePortrait = "hu_f_99_";

        private static readonly FrozenDictionary<int, RaceInfo> DefaultCreatureRaceData = new Dictionary<int, RaceInfo>()
        {
            {0, new(){MinimumAge = 40, MaximumAge = 460}},  // Dwarf
            {1, new(){MinimumAge=110, MaximumAge=750}},     // Elf
            {2, new(){MinimumAge = 40, MaximumAge = 350}},  // Gnome
            {3, new(){MinimumAge=20, MaximumAge = 150}},    // Halfling
            {4, new(){MinimumAge=20, MaximumAge = 180}},    // Half-Elf
            {5, new(){MinimumAge=14, MaximumAge=75} },      // Half-Orc    
            {6, new(){MinimumAge = 15, MaximumAge = 90}}    // Human
            // add more player-available races in .cfg file
        }.ToFrozenDictionary();

        
        public bool IsValid(out string? error)
        {
            error = null;

            if (CreatureRaceData.Count == 0)
            {
                error = $"{nameof(CreatureRaceData)} dictionary is empty, but it must contain all vanilla race IDs.";
                return false;
            }

            var errors = new List<string>();

            HashSet<int> raceIDs = new();

            foreach (var id in CreatureRaceData.Keys)
            {
                var race = NwRace.FromRaceId(id);

                if (race is null)
                {
                    errors.Add($"'{id}' is invalid race identifier.");
                }
                else if (!raceIDs.Add(race.Id))
                {
                    errors.Add($"Duplicate configuration for race " + race.Name);
                    break;
                }

            }

            if (errors.Count > 0)
            {
                error = string.Join("\n", errors);
                return false;
            }

            return true;
        }

        public void Coerce(){}

        public class RaceInfo
        {
            public string OverrideDefaultMalePortrait { get; set; } = string.Empty;
            public string OverrideDefaultFemalePortrait { get; set; } = string.Empty;
            public int MinimumAge { get; set; }
            public int MaximumAge { get; set; }
            //...
        }

        public Dictionary<int, RaceInfo> CreatureRaceData { get; set; } = DefaultCreatureRaceData.ToDictionary();

        internal RaceInfo? this[NwCreature nwCreature]
        {
            get
            {
                if (CreatureRaceData.TryGetValue(nwCreature.Race.Id, out RaceInfo? data))
                    return data;

                return null;
            }
        }
    }
}