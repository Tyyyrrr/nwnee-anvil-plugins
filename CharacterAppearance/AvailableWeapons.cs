using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;

namespace CharacterAppearance
{
    internal static class AvailableWeapons
    {
        private sealed class WeaponParts
        {
            private readonly FrozenDictionary<ItemAppearanceWeaponModel, FrozenDictionary<int, int[]>> _parts; 
            public IReadOnlyList<int> GetPartsForModel(ItemAppearanceWeaponModel model) => _parts[model].Keys;
            public IReadOnlyList<int> GetVariantsForPart(ItemAppearanceWeaponModel model, int part) => (_parts.TryGetValue(model, out var variants) && variants.TryGetValue(part, out var list)) ? list : Array.Empty<int>();
            
            public WeaponParts(Dictionary<int, int[]> bottom, Dictionary<int, int[]> middle, Dictionary<int, int[]> top)
            {
                FrozenDictionary<int, int[]> b = bottom.ToFrozenDictionary();
                FrozenDictionary<int, int[]> m = (middle == bottom) ? b : middle.ToFrozenDictionary();
                FrozenDictionary<int, int[]> t = (top == bottom) ? b : (top == middle) ? m : top.ToFrozenDictionary();

                _parts = new Dictionary<ItemAppearanceWeaponModel, FrozenDictionary<int, int[]>>()
                {
                    {ItemAppearanceWeaponModel.Bottom, b},
                    {ItemAppearanceWeaponModel.Middle, m},
                    {ItemAppearanceWeaponModel.Top, t}
                }.ToFrozenDictionary();
            }
        }

        private static FrozenDictionary<int, WeaponParts>? _availableWeaponParts;
        private static FrozenDictionary<BaseItemType, IReadOnlyList<int>>? _availableShieldModels;

        public static IReadOnlyList<int> GetAvailableWeaponParts(NwItem weapon, ItemAppearanceWeaponModel model)
        {
            if(_availableWeaponParts != null && _availableWeaponParts.TryGetValue((int)weapon.BaseItem.ItemType, out var weaponParts))
                return weaponParts.GetPartsForModel(model);

            return Array.Empty<int>();
        }

        public static IReadOnlyList<int> GetAvailableVariantsForWeaponPart(NwItem weapon, ItemAppearanceWeaponModel model, int part)
        {
            if(_availableWeaponParts != null && _availableWeaponParts.TryGetValue((int)weapon.BaseItem.ItemType, out var weaponParts))
                return weaponParts.GetVariantsForPart(model, part);

            return Array.Empty<int>();
        }


        public static IReadOnlyList<int> GetAvailableShieldParts(NwItem shield)
        {
            if(_availableShieldModels != null && _availableShieldModels.TryGetValue(shield.BaseItem.ItemType, out var list))
                return list;

            return Array.Empty<int>();
        }

        public static async void CacheAvailableWeaponPartsAndShieldModelsAsync()
        {
            var t1 = CacheWeaponPartsTask();
            var t2 = CacheShieldModelsTask();

            await NwTask.WhenAll(t1, t2);

            var str1 = "\nCollected weapon parts in " + t1.Result.TotalMilliseconds.ToString() + "ms";    
            var str2 = "Collected shield models in " + t2.Result.TotalMilliseconds.ToString() + "ms";    

            NLog.LogManager.GetCurrentClassLogger().Info(str1+"\n"+str2);
        }


        private static Task<TimeSpan> CacheWeaponPartsTask()
        {
            Stopwatch st = new();
            st.Start();
            
            var iap = ServerData.DataProviders.ItemAppearanceProvider;
            var cbits = ServerData.DataProviders.CustomBaseItemTypesMap.Values;

            var dict = new Dictionary<int, WeaponParts>();

            foreach(var bit in Enum.GetValues<BaseItemType>().Select(e=>(int)e).Concat(cbits))
            {                
                var models = iap.CollectWeaponModels(bit);

                if(models.Length != 3) continue;

                var parts = new WeaponParts(models[0],models[1],models[2]);

                dict.Add(bit, parts);
            }

            _availableWeaponParts = dict.ToFrozenDictionary();

            st.Stop();

            return Task.FromResult(st.Elapsed);
        }

        private static Task<TimeSpan> CacheShieldModelsTask()
        {
            Stopwatch st = new();
            st.Start();

            var iap = ServerData.DataProviders.ItemAppearanceProvider;

            var dict = iap.CollectShieldModels();

            _availableShieldModels = dict.ToFrozenDictionary();

            st.Stop();

            return Task.FromResult(st.Elapsed);
        }

    }
}