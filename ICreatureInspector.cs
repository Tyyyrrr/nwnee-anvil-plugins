using Anvil.API;

namespace ServerData
{
    public interface ICreatureInspector
    {
        // Races
        public bool IsBrownie(NwCreature creature);
        public bool IsAberration(NwCreature creature);
        public bool IsVanir(NwCreature creature);

        // Subraces
        public bool IsFairy(NwCreature creature);
        public bool IsMischiefling(NwCreature creature);
        public bool IsVampire(NwCreature creature);
        public bool IsLycantrophe(NwCreature creature);
        public bool IsNeur(NwCreature creature);
        public bool IsSatyr(NwCreature creature);
        public bool IsNymph(NwCreature creature);
        public bool IsWaterNymph(NwCreature creature);
        public bool IsSkulldwarf(NwCreature creature);
        public bool IsCitydwarf(NwCreature creature);
        public bool IsDesertElf(NwCreature creature);
        public bool IsUnderdweller(NwCreature creature);
        public bool IsSvart(NwCreature creature);
        public bool IsWolffolk(NwCreature creature);
        public bool IsCossack(NwCreature creature);
        public bool IsTatar(NwCreature creature);
        public bool IsGypsy(NwCreature creature);
        public bool IsAsian(NwCreature creature);
        public bool IsArabian(NwCreature creature);

        // Body state
        public bool IsFlying(NwCreature creature);
        public bool HasLegs(NwCreature creature);
        public bool HasTail(NwCreature creature);
        public bool HasWings(NwCreature creature);
        public bool IsCrawling(NwCreature creature);
    }
}