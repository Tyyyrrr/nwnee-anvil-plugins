using System;

namespace CharacterAppearance
{
    [Flags]
    public enum EditorFlags : ushort
    {
        None = 0,

        Phenotype = 1 << 0,
        Head = 1 << 1,
        Tattoo = 1 << 2,

        HairColor = 1 << 3,
        SkinColor = 1 << 4,
 
        ArmorLight = 1 << 5,
        ArmorMedium = 1 << 6,
        ArmorHeavy = 1 << 7,

        WeaponMelee = 1 << 8,
        WeaponRanged = 1 << 9,
        WeaponMagic = 1 << 10,

        BodyTailor = 1 << 11,
        FreeOfCharge = 1 << 12,

        // Flag combinations for opening editor in different modes from scripts:
        Armor = ArmorLight | ArmorMedium | ArmorHeavy,
        Weapon = WeaponMelee | WeaponRanged | WeaponMagic,
        Barber = HairColor | Head,
        BarberAndTattoos = Barber | Tattoo,
        ArmorLightAndWeaponMagic = ArmorLight | WeaponMagic,
        ArmorMediumAndWeaponRanged = ArmorMedium | WeaponRanged,
        ArmorHeavyAndWeaponMelee = ArmorHeavy | WeaponMelee,

        // additional combinations for easier use of DM commands:
        ArmorAndWeapon = Armor | Weapon,
        BodyAndArmor = BodyTailor | Armor,
        BodyAndWeapon = BodyTailor | Weapon,
        
        All = ushort.MaxValue,
    }
}