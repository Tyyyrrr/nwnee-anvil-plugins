using System.Collections.Generic;
using EasyConfig;

namespace CharacterIdentity.Configuration
{
    [ConfigFile("PolymorphCreatureNames")]
    public sealed class PolymorphCreatureNamesConfig : IConfig
    {
        public string this[int raceId]
        {
            get
            {
                if (Map.TryGetValue(raceId, out var value)) return value;
                return "Nieznana przemiana";
            }
        }
        
        
        public Dictionary<int, string> Map { get; set; } = new();

        public void Coerce() { }
        public bool IsValid(out string? error) { error = null; return true; }
    }
}

