using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;

using EasyConfig;

namespace MovementSystem.Configuration
{

    [ConfigFile("Surface")]
    public sealed class SurfaceConfig : IConfig
    {
        public float MinValue{get;set;} = -10f;
        public float MaxValue{get;set;} = 10f;

        public Dictionary<string, float> MaterialMovementSpeed {get;set;}
        public Dictionary<string, string> MaterialNames {get;set;}

        private FrozenDictionary<int, float>? _materialMovementSpeed = null;
        private FrozenDictionary<int, string>? _materialNames = null;
        private FrozenDictionary<int, string>? _materialOriginalNames = null;

        public SurfaceConfig()
        {
            var surfaceTab = NwGameTables.SurfaceMaterialTable;

            MaterialMovementSpeed = surfaceTab
                .Where(e=>e.Walk == true && e.Label != null)
                .Select(e=>new KeyValuePair<string, float>(e.Label!, 0f))
                .ToDictionary();

            MaterialNames = surfaceTab
                .Where(e=>e.Label != null && MaterialMovementSpeed.ContainsKey(e.Label))
                .Select(e=>new KeyValuePair<string, string>(e.Label!, string.Empty))
                .ToDictionary();
        }

        public void Coerce()
        {
            var keys = MaterialMovementSpeed.Where(kvp => kvp.Value < MinValue || kvp.Value > MaxValue).Select(kvp=>kvp.Key).ToArray();

            foreach(var key in keys)
            {
                var val = MaterialMovementSpeed[key];
                MaterialMovementSpeed[key] = MathF.Max(MathF.Min(val, MaxValue), MinValue);
            }

            var surfaceTab = NwGameTables.SurfaceMaterialTable;

            _materialMovementSpeed = surfaceTab
                .Where(e=>e.Label != null && MaterialMovementSpeed.ContainsKey(e.Label))
                .Select(e=>new KeyValuePair<int, float>(e.RowIndex, MaterialMovementSpeed[e.Label!]))
                .ToFrozenDictionary();

            _materialNames = surfaceTab
                .Where(e=>_materialMovementSpeed.ContainsKey(e.RowIndex))
                .Select(e=>new KeyValuePair<int, string>(e.RowIndex, MaterialNames[e.Label!]))
                .ToFrozenDictionary();

            _materialOriginalNames = surfaceTab
                .Where(e=>_materialMovementSpeed.ContainsKey(e.RowIndex) && e.Label != null)
                .Select(e=>new KeyValuePair<int, string>(e.RowIndex, e.Label!))
                .ToFrozenDictionary();
        }

        internal string GetName(int surface) => _materialNames?.TryGetValue(surface, out var name) ?? false ? name : "Nieznane podłoże";

        public bool IsValid(out string? error) 
        {
            error = MaterialMovementSpeed.Keys.Count == 0 ? "Dictionary has no keys." : _materialMovementSpeed == null ? "Not indexed." : null;

            return error == null;
        }
        

        public float GetSpeedModifierForSurface(int surface)
        {
            if(_materialMovementSpeed == null || !_materialMovementSpeed.TryGetValue(surface, out var val))
                return 0f;

            return val;
        }

        public string? GetMaterialOriginalName(int surface) => (_materialOriginalNames?.TryGetValue(surface, out var name) ?? false) ? name : null; 
    }
}