using EasyConfig;

namespace CharacterAppearance
{
    [ConfigFile("Editor")]
    public sealed class EditorConfig : IConfig
    {
        public float ArmorEditCostMultiplierMax {get;set;} = 0.2f;
        public float ArmorEditCostMultiplierMin {get;set;} = 0.05f;
        public float ArmorEditColorToPartRatio {get;set;} = 0.2f;
        public float WeaponEditCostMultiplierMax {get;set;} = 0.2f;
        public float WeaponEditCostMultiplierMin {get;set;} = 0.05f;



        public int HairChangeCost {get;set;} = 1000;
        public int HairColorChangeCost {get;set;} = 500;
        public int TattooCreateCost {get;set;} = 2000;
        public int TattooRemoveCost {get;set;} = 15000;
        public int TattooColorChangeCost {get;set;} = 1500;

        public void Coerce(){}

        public bool IsValid(out string? error) {error = null; return true;}
    }
}