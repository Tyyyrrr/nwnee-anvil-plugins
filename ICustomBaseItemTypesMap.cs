using System.Collections.Generic;

namespace ServerData
{
    public interface ICustomBaseItemTypesMap
    {
        public IReadOnlyList<int> Values {get;}
        
        // weapons:
        int Trident1H {get;}
        int HeavyPick {get;}
        int LightPick {get;}
        int Sai {get;}
        int Nunchaku {get;}
        int Falchion {get;}
        int Sap {get;}
        int AssassinDagger {get;}
        int Katar {get;}
        int Falchion2 {get;}
        int HeavyMace {get;}
        int Maul2H {get;}
        int QuicksilverSword1H {get;}
        int QuicksilverSword2H {get;}
        int DoubleScimitar {get;}
        int BoatHook {get;}
        int WindFireWheel {get;}
    }
}