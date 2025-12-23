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

        // Body state
        public bool IsFlying(NwCreature creature);
        public bool HasLegs(NwCreature creature);
        public bool HasTail(NwCreature creature);
        public bool HasWings(NwCreature creature);
        public bool IsCrawling(NwCreature creature);
    }
}