using System.Collections.Generic;

namespace ServerData
{
    public interface ICustomFeatsMap
    {
        public IReadOnlyList<int> Values {get;}
        public int FencerForth {get;}
        public int MoonhunterBloodCall {get;}
        public int EpicSpellFleetnessOfFoot {get;}
        public int Gadabout {get;}
        public int LivelyStep {get;}
        public int RogueFastFoot {get;}
        public int LightingShadow {get;}
        public int TravelForm {get;}
        public int SteadfastDefender {get;}
        public int WildAgility {get;}        

        public int FencerAlleSpellID{get;}
        public int SteadfastDefenderSpellID{get;}

        public bool IsTravelFormAppearanceType(int appearanceType);
    }
}