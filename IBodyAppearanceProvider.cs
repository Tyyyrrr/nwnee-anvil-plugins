using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace ServerData
{
    public interface IBodyAppearanceProvider
    {
        public static readonly IReadOnlyList<int> AllColors = Enumerable.Range(0,11*16).ToArray();
        public IReadOnlyList<int> GetSkinColorsForCreature(NwCreature creature);
        public (float,float) GetMinMaxBodyHeightForCreature(NwCreature creature);    
        public bool IsDwarfAppearanceType(int appearanceType);
        public bool IsElfOrBrownieAppearanceType(int appearanceType);
        public bool IsGnomeAppearanceType(int appearanceType);
        public bool IsHalflingAppearanceType(int appearanceType);
        public bool IsHalfOrcAppearanceType(int appearanceType);
        public bool IsHumanOrHalfElfAppearanceType(int appearanceType);
        public bool IsOrcAppearanceType(int appearanceType);
        public bool IsGoblinAppearanceType(int appearanceType);
        public bool IsKoboldAppearanceType(int appearanceType);
        public bool IsVanirAppearanceType(int appearanceType);

        public IReadOnlyList<int> GetHeadsForRace(int raceID, Gender gender);
        public IReadOnlyList<int> GetAppearanceTypesForRace(int raceID);
        public IReadOnlyList<int> GetHeadsForAberrationAppearance(int appearanceType, Gender gender);

        public IReadOnlyDictionary<CreaturePart, IList<int>>? GetMiscellaneousBodyPartsForCreature(NwCreature creature);
    }
}