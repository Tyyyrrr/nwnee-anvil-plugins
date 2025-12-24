using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Anvil.API;
using EasyConfig;
using NWN.Core;

namespace MovementSystem.Configuration
{
    [ConfigFile("Feats")]
    public sealed class FeatsConfig : IConfig
    {
        public Dictionary<string, float> ActiveFeats {get;set;} = new()
        {
            {nameof(NWScript.FEAT_EPIC_BLINDING_SPEED), 0.5f},
            {ServerData.DataProviders.CustomFeatsMap.FencerForth.ToString(), 1.0f},
            {ServerData.DataProviders.CustomFeatsMap.MoonhunterBloodCall.ToString(), 0.01f},
            {ServerData.DataProviders.CustomFeatsMap.EpicSpellFleetnessOfFoot.ToString(), 1.0f},
            {ServerData.DataProviders.CustomFeatsMap.SteadfastDefender.ToString(), -1f}
        };
        public Dictionary<string, float> PassiveFeats {get;set;} = new()
        {

            {ServerData.DataProviders.CustomFeatsMap.Gadabout.ToString(), 0.01f},
            {ServerData.DataProviders.CustomFeatsMap.LivelyStep.ToString(), 0.01f},
            {ServerData.DataProviders.CustomFeatsMap.RogueFastFoot.ToString(), 0.01f},
            {ServerData.DataProviders.CustomFeatsMap.LightingShadow.ToString(), 0.25f},
            {nameof(NWScript.FEAT_IMPROVED_INITIATIVE), 0.05f},
            {nameof(NWScript.FEAT_EPIC_SUPERIOR_INITIATIVE), 0.1f},
            {ServerData.DataProviders.CustomFeatsMap.TravelForm.ToString(), 0.33f},
        };
        public Dictionary<string, string> FeatNames {get;set;} = new()
        {            
            {nameof(NWScript.FEAT_EPIC_BLINDING_SPEED), "Oślepiająca szybkość"},
            {ServerData.DataProviders.CustomFeatsMap.FencerForth.ToString(), "Alle!"},
            {ServerData.DataProviders.CustomFeatsMap.MoonhunterBloodCall.ToString(), "Zew krwi"},
            {ServerData.DataProviders.CustomFeatsMap.EpicSpellFleetnessOfFoot.ToString(), "Chyże stopy"},
            {ServerData.DataProviders.CustomFeatsMap.Gadabout.ToString(), "Powsinoga"},
            {ServerData.DataProviders.CustomFeatsMap.LivelyStep.ToString(), "Żwawy krok"},
            {ServerData.DataProviders.CustomFeatsMap.RogueFastFoot.ToString(), "Chyżostopy"},
            {ServerData.DataProviders.CustomFeatsMap.LightingShadow.ToString(), "Błyskawiczny cień"},
            {nameof(NWScript.FEAT_IMPROVED_INITIATIVE), "Ulepszona inicjatywa"},
            {nameof(NWScript.FEAT_EPIC_SUPERIOR_INITIATIVE), "Niesamowita inicjatywa"},
            {ServerData.DataProviders.CustomFeatsMap.TravelForm.ToString(), "Postać wędrowca"},
            {ServerData.DataProviders.CustomFeatsMap.SteadfastDefender.ToString(), "Niezłomny defensor"}
        };


        
        private FrozenDictionary<int, float>? _activeFeats;
        private FrozenDictionary<int, float>? _passiveFeats;

        public float GetFeatSpeedModifier(int featId)
        {
            if(_activeFeats == null || _passiveFeats == null || !(_activeFeats.TryGetValue(featId, out var val) || _passiveFeats.TryGetValue(featId, out val)))
                return 0f;
                
            return val;
        }

        private FrozenDictionary<int, string>? _featNames;
        public string? GetFeatName(int featId)
        {
            if(_featNames == null) return null;

            _ = _featNames.TryGetValue(featId, out var name);

            return name;
        }

        [JsonIgnore] public IEnumerable<int> MovementAffectingActiveFeats => _activeFeats?.Keys ?? throw new InvalidOperationException("MovementAffectingActiveFeats collection is not initialized.");
        [JsonIgnore] public IEnumerable<int> MovementAffectingPassiveFeats => _passiveFeats?.Keys ?? throw new InvalidOperationException("MovementAffectingPassiveFeats collection is not initialized.");

        public FeatsConfig(){}

        public void Coerce()
        {
            var featsTab = NwGameTables.GetTable("feat") ?? throw new InvalidOperationException("feat.2da is missing");

            var actives = new Dictionary<int, float>();
            var passives = new Dictionary<int, float>();
            var names = new Dictionary<int, string>();

            var constColId = featsTab.GetColumnIndex("Constant");
            for(int i = 0; i < featsTab.RowCount; i++)
            {
                var e = featsTab.GetString(i, constColId);

                if(string.IsNullOrEmpty(e)) continue;
                    
                if(ActiveFeats.TryGetValue(e, out var val)) 
                {
                    actives.Add(i,val);
                }
                
                else if (PassiveFeats.TryGetValue(e, out val)) passives.Add(i, val);

                else continue;

                if(FeatNames.TryGetValue(e, out var name)) names.Add(i, name);
            }

            _activeFeats = actives.ToFrozenDictionary();
            _passiveFeats = passives.ToFrozenDictionary();

            _featNames = names.ToFrozenDictionary();
        }

        public bool IsValid(out string? error) 
        {
            error = null;

            if(_activeFeats == null || _activeFeats.Count == 0)
            {
                error = "No active feats";
            }
            
            if(_passiveFeats == null || _passiveFeats.Count == 0)
            {
                error = error == null ? "No passive feats" : error + "\nNo passive feats";
            }

            return error == null;
        }
        
    }
}