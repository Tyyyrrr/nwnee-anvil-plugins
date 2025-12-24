using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using ExtensionsPlugin;
using NWN.Core;

namespace CharacterAppearance.Wrappers
{
    
    internal sealed class WeaponItemWrapper : ItemWrapper
    {
        private readonly int[] _supportedVanillaItemTypes;
        private readonly int[] _supportedCustomItemTypes;

        private readonly EditorFlags _flags;
        public WeaponItemWrapper(EditorFlags flags)
        {
            _flags = flags;

            List<int> suppVanillaTypes = new();
            List<int> suppCustomTypes = new();

            if (flags.HasFlag(EditorFlags.WeaponMelee))
            {
                suppVanillaTypes.AddRange(new int[]
                {
                   (int)BaseItemType.Bastardsword,
                   (int)BaseItemType.Battleaxe,
                   (int)BaseItemType.Club,
                   (int)BaseItemType.Dagger,
                   (int)BaseItemType.DireMace,
                   (int)BaseItemType.Doubleaxe,
                   (int)BaseItemType.DwarvenWaraxe,
                   (int)BaseItemType.Greataxe,
                   (int)BaseItemType.Greatsword,
                   (int)BaseItemType.Greatsword,
                   (int)BaseItemType.Halberd,
                   (int)BaseItemType.Handaxe, 
                   (int)BaseItemType.HeavyFlail,
                   (int)BaseItemType.Kama,
                   (int)BaseItemType.Katana,
                   (int)BaseItemType.Kukri,
                   (int)BaseItemType.LargeShield,
                   (int)BaseItemType.LightFlail,
                   (int)BaseItemType.LightHammer,
                   (int)BaseItemType.LightMace,
                   (int)BaseItemType.Longsword,
                   (int)BaseItemType.Morningstar,
                   (int)BaseItemType.Quarterstaff,
                   (int)BaseItemType.Rapier,
                   (int)BaseItemType.Scimitar,
                   (int)BaseItemType.Scythe,
                   (int)BaseItemType.ShortSpear,
                   (int)BaseItemType.Shortsword,
                   (int)BaseItemType.Sickle,
                   (int)BaseItemType.SmallShield,
                   (int)BaseItemType.TowerShield,
                   (int)BaseItemType.Trident,
                   (int)BaseItemType.TwoBladedSword,
                   (int)BaseItemType.Warhammer,
                   (int)BaseItemType.Whip,
                });

                var cbit = ServerData.DataProviders.CustomBaseItemTypesMap;

                suppCustomTypes.AddRange(new int[]
                {
                   cbit.AssassinDagger,
                   cbit.BoatHook,
                   cbit.DoubleScimitar,
                   cbit.Falchion,
                   cbit.Falchion2,
                   cbit.Maul2H,
                   cbit.HeavyMace,
                   cbit.HeavyPick,
                   cbit.Katar,
                   cbit.LightPick,
                   cbit.Nunchaku,
                   cbit.QuicksilverSword1H,
                   cbit.QuicksilverSword2H,
                   cbit.Sai,
                   cbit.Sap,
                   cbit.Trident1H,
                   cbit.WindFireWheel
                });
            }

            if(flags.HasFlag(EditorFlags.WeaponRanged))
            {
                suppVanillaTypes.AddRange(new int[]
                {
                   (int)BaseItemType.Dart,
                   (int)BaseItemType.HeavyCrossbow,
                   (int)BaseItemType.LightCrossbow,
                   (int)BaseItemType.Shortbow,
                   (int)BaseItemType.Shuriken,
                   (int)BaseItemType.Sling,
                   (int)BaseItemType.ThrowingAxe,
                });
            }

            if(flags.HasFlag(EditorFlags.WeaponMagic))
            {
                suppVanillaTypes.AddRange(new int[]
                {
                   (int)BaseItemType.MagicRod,
                   (int)BaseItemType.MagicStaff,
                });
            }

            _supportedVanillaItemTypes = suppVanillaTypes.ToArray();
            _supportedCustomItemTypes = suppCustomTypes.ToArray();
        }

        private bool IsItemSupported(NwItem item)
        {
            if(!item.IsValid) return false;
            int bit = NWScript.GetBaseItemType(item.ObjectId);
            return _supportedCustomItemTypes.Contains(bit) || _supportedVanillaItemTypes.Contains(bit);
        }

        public bool IsSupportedItemType {get;private set;} = false;

        public int Top, Mid, Bot, ShieldModel = -1;
        public int OriginalTop, OriginalMid, OriginalBot, OriginalShieldModel;
        public bool IsShield {get;private set;}
        public bool IsReEquippingAfterAppearanceChange {get;private set;} = false;

        public override NwItem Item 
        { 
            get => base.Item; 
            set 
            {                
                if(HasItem && base.Item == value && IsSupportedItemType)
                    return;

                base.Item = value; 

                IsSupportedItemType = IsItemSupported(value);

                if (!HasItem || !IsSupportedItemType) {
                    ClearItem();
                    return;
                }
            }
        }

        public override void ClearItem()
        {
            base.ClearItem();

            ResetWeaponValues();
            ResetShieldValues();

            IsSupportedItemType = false;
            IsReEquippingAfterAppearanceChange = false;
        }

        public void CacheCurrentAppearance()
        {
            if (!HasItem || !IsSupportedItemType)
            {
                ClearItem();
                return;
            }

            if (Item.IsShield())
            {
                OriginalShieldModel = Item.Appearance.GetSimpleModel();
                ShieldModel = OriginalShieldModel;
                IsShield = true;
                ResetWeaponValues();
                return;
            }

            ResetShieldValues();

            OriginalBot = Item.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
            OriginalMid = Item.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            OriginalTop = Item.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);

            Bot = OriginalBot;
            Mid = OriginalMid;
            Top = OriginalTop;
        }

        public int GetEditCost()
        {
            if(!HasItem || !IsAppearanceDirty || !IsSupportedItemType) return 0;

            var minMax = CharacterAppearanceService.WeaponEditCostMultiplierMinMax;
            int gpVal = Item.GoldValue;
            var minMaxCost = (gpVal * minMax.Item1, gpVal*minMax.Item2);

            if (IsShield)
            {
                return ShieldModel == OriginalShieldModel ? 0 : (int)Math.Max(1,minMaxCost.Item2);
            }
            int partsChanged = (Top == OriginalTop ? 0 : 1) + (Mid == OriginalMid ? 0 : 1) + (Bot == OriginalBot ? 0 : 1);

            if(partsChanged == 0) return 0;

            float cost = ((float)partsChanged)/3 * minMaxCost.Item2;

            cost = Math.Max(minMaxCost.Item1, Math.Min(minMaxCost.Item2, cost));
            
            return Math.Max(1,(int)Math.Round(cost, 0));
        }

        public bool IsAppearanceDirty => IsShield 
            ? (ShieldModel != OriginalShieldModel)
            : (Bot != OriginalBot || Mid != OriginalMid || Top != OriginalTop);

        public override void RestoreOriginal()
        {
            if(!HasItem || !IsAppearanceDirty) return;
            if (IsShield)
            {
                IsReEquippingAfterAppearanceChange = true;
                Item = Item.Appearance.ChangeAppearance((a)=>{a.SetSimpleModel((ushort)OriginalShieldModel);});
                IsReEquippingAfterAppearanceChange = false;
                ShieldModel = OriginalShieldModel;
                ResetWeaponValues();
                RestoreOriginalUUID();
                return;
            }

            IsReEquippingAfterAppearanceChange = true;
            Item = Item.Appearance.ChangeAppearance((a) =>
            {
                if(Bot != OriginalBot) a.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (ushort)OriginalBot); 
                if(Mid != OriginalMid) a.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (ushort)OriginalMid);
                if(Top != OriginalTop) a.SetWeaponModel(ItemAppearanceWeaponModel.Top, (ushort)OriginalTop);
            });
            IsReEquippingAfterAppearanceChange = false;

            Bot = OriginalBot;
            Mid = OriginalMid;
            Top = OriginalTop;
            ResetShieldValues();
            RestoreOriginalUUID();
        }

        public void SetWeaponModel(ItemAppearanceWeaponModel part, ushort model)
        {
            if(IsShield) return;

            switch (part)
            {
                case ItemAppearanceWeaponModel.Bottom:
                    if(model == Bot) return;
                    else Bot = model; break;

                case ItemAppearanceWeaponModel.Middle:
                    if(model == Mid) return;
                    else Mid = model; break;

                case ItemAppearanceWeaponModel.Top:
                    if(model == Top) return;
                    else Top = model; break;

                default: return;
            }

            IsReEquippingAfterAppearanceChange = true;
            Item = Item.Appearance.ChangeAppearance((a)=>a.SetWeaponModel(part, model));
            IsReEquippingAfterAppearanceChange = false;

            ResetShieldValues();
            RestoreOriginalUUID();
        }

        void ResetWeaponValues()
        {
            OriginalBot = -1; Bot = -1;
            OriginalMid = -1; Mid = -1;
            OriginalTop = -1; Top = -1;
        }

        void ResetShieldValues()
        {
            OriginalShieldModel = -1; 
            ShieldModel = -1; 
            IsShield = false;
        }

        public void SetShieldModel(ushort model)
        {
            if(model == ShieldModel || !IsShield) return;

            ShieldModel = model;

            IsReEquippingAfterAppearanceChange = true;
            Item = Item.Appearance.ChangeAppearance((a)=>{a.SetSimpleModel(model);});
            IsReEquippingAfterAppearanceChange = false;
            ResetWeaponValues();
            RestoreOriginalUUID();
        }

        public override void MarkAsOriginal()
        {
            base.MarkAsOriginal();

            if(!HasItem || !IsSupportedItemType)
            {
                ClearItem();
                return;
            }

            OriginalTop = Top;
            OriginalBot = Bot;
            OriginalMid = Mid;

            OriginalShieldModel = ShieldModel;
        }
    }

}