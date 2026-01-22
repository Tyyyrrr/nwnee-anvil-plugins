using Anvil.API;
using ServerData;

namespace ExtensionsPlugin
{
    public static class NwCreatureExtensions
    {
        private static NwCreatureExtensionsConfig _config = new();
        internal static void CacheConfig(EasyConfig.ConfigurationService easyCfg)
        {
            var config = easyCfg.GetConfig<NwCreatureExtensionsConfig>();
            if (config != null)
                _config = config;
        }



        public static string GetDefaultPortrait(this NwCreature creature, Gender overrideGender = Gender.None)
        {
            var raceInfo = _config[creature];
            if (raceInfo != null)
            {
                var gen = creature.Gender == overrideGender || (overrideGender != Gender.Male && overrideGender != Gender.Female) ? creature.Gender : overrideGender;
                var ovr = gen == Gender.Male ? raceInfo.OverrideDefaultMalePortrait : raceInfo.OverrideDefaultMalePortrait;

                if (!string.IsNullOrEmpty(ovr))
                    return ovr;
            }
            
            return creature.Gender == Gender.Male ? NwCreatureExtensionsConfig.DefaultHumanMalePortrait : NwCreatureExtensionsConfig.DefaultHumanFemalePortrait;
        }


        public static string GetDefaultPortraitResRef(this NwCreature creature, Gender overrideGender = Gender.None) => "po_" + creature.GetDefaultPortrait(overrideGender);
        public static string GetDefaultPortraitResRef_Tiny(this NwCreature creature, Gender overrideGender = Gender.None) => "po_" + creature.GetDefaultPortrait(overrideGender) + 't';
        public static string GetDefaultPortraitResRef_Small(this NwCreature creature, Gender overrideGender = Gender.None) => "po_" + creature.GetDefaultPortrait(overrideGender) + 's';
        public static string GetDefaultPortraitResRef_Medium(this NwCreature creature, Gender overrideGender = Gender.None) => "po_" + creature.GetDefaultPortrait(overrideGender) + 'm';
        public static string GetDefaultPortraitResRef_Large(this NwCreature creature, Gender overrideGender = Gender.None) => "po_" + creature.GetDefaultPortrait(overrideGender) + 'l';
        public static string GetDefaultPortraitResRef_Huge(this NwCreature creature, Gender overrideGender = Gender.None) => "po_" + creature.GetDefaultPortrait(overrideGender) + 'h';


        public static int GetMinimumAge(this NwCreature creature)
        {
            var raceInfo = _config[creature];
            if (raceInfo != null)
                return raceInfo.MinimumAge;
            else if (_config.CreatureRaceData.TryGetValue((int)RacialType.Human, out var humanRaceInfo))
                return humanRaceInfo.MinimumAge;
            else return 0;
        }

        public static int GetMaximumAge(this NwCreature creature)
        {
            var raceInfo = _config[creature];
            if (raceInfo != null)
                return raceInfo.MaximumAge;
            else if (_config.CreatureRaceData.TryGetValue((int)RacialType.Human, out var humanRaceInfo))
                return humanRaceInfo.MaximumAge;
            else return 0;
        }

        public static bool IsFlying(this NwCreature creature) => DataProviders.CreatureInspector.IsFlying(creature);
        public static bool HasTail(this NwCreature creature) => DataProviders.CreatureInspector.HasTail(creature);
        public static bool HasWings(this NwCreature creature) => DataProviders.CreatureInspector.HasWings(creature);   
        public static bool HasLegs(this NwCreature creature) => DataProviders.CreatureInspector.HasLegs(creature);
        public static bool IsCrawling(this NwCreature creature) => DataProviders.CreatureInspector.IsCrawling(creature);
        public static bool IsBrownie(this NwCreature creature) => DataProviders.CreatureInspector.IsBrownie(creature);
        public static bool IsAberration(this NwCreature creature) => DataProviders.CreatureInspector.IsAberration(creature);
        public static bool IsVanir(this NwCreature creature) => DataProviders.CreatureInspector.IsVanir(creature);
        public static bool IsMischiefling(this NwCreature creature) => DataProviders.CreatureInspector.IsMischiefling(creature);
        public static bool IsFairy(this NwCreature creature) => DataProviders.CreatureInspector.IsFairy(creature);
        public static bool IsPixie(this NwCreature creature) => IsFairy(creature) || IsMischiefling(creature);
        public static bool IsNymph(this NwCreature creature) => DataProviders.CreatureInspector.IsNymph(creature);
        public static bool IsWaterNymph(this NwCreature creature) => DataProviders.CreatureInspector.IsWaterNymph(creature);
        public static bool IsSatyr(this NwCreature creature) => DataProviders.CreatureInspector.IsSatyr(creature);
        public static bool IsNeur(this NwCreature creature) => DataProviders.CreatureInspector.IsNeur(creature);
        public static bool IsVampire(this NwCreature creature) => DataProviders.CreatureInspector.IsVampire(creature);
        public static bool IsLycantrophe(this NwCreature creature) => DataProviders.CreatureInspector.IsLycantrophe(creature);

        public static bool IsSkulldwarf(this NwCreature creature) => DataProviders.CreatureInspector.IsSkulldwarf(creature);
        public static bool IsCitydwarf(this NwCreature creature) => DataProviders.CreatureInspector.IsCitydwarf(creature);
        public static bool IsDesertElf(this NwCreature creature) => DataProviders.CreatureInspector.IsDesertElf(creature);
        public static bool IsUnderdweller(this NwCreature creature) => DataProviders.CreatureInspector.IsUnderdweller(creature);
        public static bool IsSvart(this NwCreature creature) => DataProviders.CreatureInspector.IsSvart(creature);
        public static bool IsWolffolk(this NwCreature creature) => DataProviders.CreatureInspector.IsWolffolk(creature);
        public static bool IsCossack(this NwCreature creature) => DataProviders.CreatureInspector.IsCossack(creature);
        public static bool IsTatar(this NwCreature creature) => DataProviders.CreatureInspector.IsTatar(creature);
        public static bool IsGypsy(this NwCreature creature) => DataProviders.CreatureInspector.IsGypsy(creature);
        public static bool IsAsian(this NwCreature creature) => DataProviders.CreatureInspector.IsAsian(creature);
        public static bool IsArabian(this NwCreature creature) => DataProviders.CreatureInspector.IsArabian(creature);
    }
}