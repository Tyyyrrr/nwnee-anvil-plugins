using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using CharacterAppearance.Wrappers;

namespace CharacterAppearance.UI
{
    internal sealed class WeaponEditorModel : AppearanceEditorModel, IDisposable
    {
        private readonly NwCreature _pc;

        private readonly WeaponItemWrapper RWrapper;
        private readonly WeaponItemWrapper LWrapper;

        private int _weaponModel;

        int Model => _weaponModel / 10;
        int Variant => _weaponModel - Model * 10;

        public event System.Action? OnDirty;

        public WeaponEditorModel(NwCreature pc, EditorFlags flags)
        {
            _pc = pc;
            
            RWrapper = new(flags);
            LWrapper = new(flags);

            CharacterAppearanceService.EventService.Unsubscribe<OnItemEquip, OnItemEquip.Factory>(_pc, OnItemEquip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Unsubscribe<OnItemUnequip, OnItemUnequip.Factory>(_pc, OnItemUnequip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Subscribe<OnItemEquip, OnItemEquip.Factory>(_pc, OnItemEquip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Subscribe<OnItemUnequip, OnItemUnequip.Factory>(_pc, OnItemUnequip, Anvil.Services.EventCallbackType.After);

            var item = _pc.GetItemInSlot(InventorySlot.RightHand);
            if(item != null && item.IsValid && item.TryGetUUID(out var guid))
            {
                RWrapper.Item = item;
                if(RWrapper.HasItem && RWrapper.IsSupportedItemType)
                {
                    RWrapper.OriginalGuid = guid;
                    RWrapper.CacheCurrentAppearance();
                    RWrapper.MarkAsOriginal();
                }
            }

            item = _pc.GetItemInSlot(InventorySlot.LeftHand);
            if(item != null && item.IsValid && item.TryGetUUID(out guid) && !(RWrapper.HasItem && RWrapper.Item == item))
            {
                LWrapper.Item = item;
                if(LWrapper.HasItem && LWrapper.IsSupportedItemType)
                {
                    LWrapper.OriginalGuid = guid;
                    LWrapper.CacheCurrentAppearance();
                    LWrapper.MarkAsOriginal();
                }
            }

            _leftSide = !RWrapper.HasItem && LWrapper.HasItem && LWrapper.IsShield && LWrapper.IsSupportedItemType;

            RefreshMainEntries();
            
            MainSelection = 0;

        }


        void RefreshMainEntries()
        {
            if(LeftSide && LWrapper.HasItem) _mainEntries = (LWrapper.IsShield && LWrapper.IsSupportedItemType) ? _shieldMainEntry : Array.Empty<string>();
            
            else if(!LeftSide && RWrapper.HasItem && RWrapper.IsSupportedItemType) _mainEntries = _weaponMainEntries;

            else _mainEntries = Array.Empty<string>();
        }

        void RefreshSubEntries()
        {
            var wrapper = LeftSide ? LWrapper : RWrapper;

            if(!wrapper.HasItem || !wrapper.IsSupportedItemType || (LWrapper == wrapper && !LWrapper.IsShield)) _subEntries.Clear();
            
            else if (wrapper.IsShield)  _subEntries = AvailableWeapons.GetAvailableShieldParts(wrapper.Item).Select(i=>i.ToString()).ToList();
            
            else if(_mainSelection < 3) _subEntries = AvailableWeapons.GetAvailableWeaponParts(wrapper.Item, SelectedWeaponPart).Select(i=>i.ToString()).ToList();
            
            else _subEntries = AvailableWeapons.GetAvailableVariantsForWeaponPart(wrapper.Item, SelectedWeaponPart, Model).Select(i=>i.ToString()).ToList();
        }

        private string[] _mainEntries = Array.Empty<string>();
        public override int MainEntriesCount => _mainEntries.Length;
        public override IEnumerable<string> MainEntries => _mainEntries;

        private int _mainSelection;
        public override int MainSelection
        {
            get => _mainSelection;
            set
            {
                var wrapper = LeftSide ? LWrapper : RWrapper;

                if (!wrapper.HasItem || !wrapper.IsSupportedItemType)
                {
                    _weaponModel = 0;
                    _mainSelection = 0;
                }
                else if (wrapper.IsShield)
                {
                    _weaponModel = wrapper.ShieldModel;
                    _mainSelection = 0;
                }
                else
                {
                    _mainSelection = Math.Max(0,Math.Min(value, MainEntriesCount - 1));
                    _weaponModel = SelectedWeaponPart switch
                    {
                        ItemAppearanceWeaponModel.Bottom => wrapper.Bot,
                        ItemAppearanceWeaponModel.Middle => wrapper.Mid,
                        _ => wrapper.Top
                    };
                }
             
                RefreshSubEntries();

                SubSelection = GetCurrentSubSelection();
            }
        }

        private List<string> _subEntries = new();
        public override int SubEntriesCount => _subEntries.Count;
        public override IEnumerable<string> SubEntries => _subEntries;


        private int GetCurrentSubSelection()
        {
            var wrapper = LeftSide ? LWrapper : RWrapper;

            if(!wrapper.HasItem || !wrapper.IsSupportedItemType) 
                return 0;

            else if (wrapper.IsShield)
            {
                if(wrapper != LWrapper) 
                    return 0;

                return Math.Max(0,_subEntries.IndexOf(wrapper.ShieldModel.ToString()));
            }

            else if(_mainSelection < 3)
                return Math.Max(0,_subEntries.IndexOf(Model.ToString()));
            
            else 
                return Math.Max(0,_subEntries.IndexOf(Variant.ToString()));
        }

        ItemAppearanceWeaponModel SelectedWeaponPart => _mainSelection switch
        {                    
            0 or 3 =>ItemAppearanceWeaponModel.Top,
            1 or 4 =>ItemAppearanceWeaponModel.Middle,
            _=>ItemAppearanceWeaponModel.Bottom  
        };

        private int _subSelection;
        public override int SubSelection
        {
            get => _subSelection;
            set
            {
                var wrapper = LeftSide ? LWrapper : RWrapper;

                if (!wrapper.HasItem || value < 0 || value >= SubEntriesCount || !wrapper.IsSupportedItemType)
                {
                    _subSelection = 0;
                    OnDirty?.Invoke();
                    return;
                }

                var strVal = _subEntries[value];

                if(!ushort.TryParse(strVal, out var val))
                {
                    _subSelection = 0;
                    OnDirty?.Invoke();
                    return;
                }

                _subSelection = value;

                int old;

                if (wrapper.IsShield)
                {
                    old = _weaponModel;
                    _weaponModel = val;
                    wrapper.SetShieldModel(val);
                    OnDirty?.Invoke();
                    return;
                }
                else if(_mainSelection < 3)
                {
                    old = Model;
                    var wrapperModel = SelectedWeaponPart switch
                    {
                        ItemAppearanceWeaponModel.Bottom => wrapper.Bot,
                        ItemAppearanceWeaponModel.Middle => wrapper.Mid,
                        _ => wrapper.Top  
                    };
                    int currentVariant = GetWeaponVariant(wrapperModel);
                    var availableVariants = AvailableWeapons.GetAvailableVariantsForWeaponPart(wrapper.Item,SelectedWeaponPart,val);
                    currentVariant = availableVariants.Contains(currentVariant) ? currentVariant : availableVariants[0];
                    _weaponModel = val * 10 + currentVariant;
                }
                else
                {
                    old = Variant;
                    _weaponModel = Model * 10 + val;
                }

                wrapper.SetWeaponModel(SelectedWeaponPart, (ushort)Math.Max(0,Math.Min(ushort.MaxValue,_weaponModel)));
                OnDirty?.Invoke();
            }
        }

        static int GetWeaponVariant(int weaponModel)
        {
            int model = weaponModel / 10;
            return weaponModel - model * 10;
        }

        // colors are turned off for weapon editor
        public override int SelectedColorChannel { get;set; } = -1;
        public override int SelectedColorIndex { get;set;} = 0;
        

        private static readonly string[] _shieldMainEntry = new string[]{"Tarcza"};
        private static readonly string[] _weaponMainEntries = new string[]{"Górna część", "Środkowa część", "Dolna część", "Wariant: góra", "Wariant: środek", "Wariant: dół"};
        private bool _leftSide;
        public override bool LeftSide
        {
            get => _leftSide;
            set 
            {
                _leftSide = value;
                RefreshMainEntries();
                MainSelection = _mainSelection;
            }
        }

        // copying to the other side is not supported by weapon editor
        public override bool IsSymmetrical => true;
        
        // make left/right side selection available when shield and weapon is equipped
        public override bool IsDoublePart => LWrapper.HasItem && RWrapper.HasItem && RWrapper.Item != LWrapper.Item && LWrapper.IsShield;

        public override ColorChart CurrentColorChart => ColorChart.Metal;

        public override void CopyToTheOtherSide() => throw new NotSupportedException("Weapon Editor does not support symmetry mirroring");
        

        public void Dispose()
        {
            CharacterAppearanceService.EventService.Unsubscribe<OnItemEquip, OnItemEquip.Factory>(_pc, OnItemEquip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Unsubscribe<OnItemUnequip, OnItemUnequip.Factory>(_pc, OnItemUnequip, Anvil.Services.EventCallbackType.After);
            RevertChanges();
            RWrapper.ClearItem();
            LWrapper.ClearItem();      
        }

        public override bool IsValidColor(int colorID) => true;


        public override int AppearanceChangeCost => RWrapper.GetEditCost() + LWrapper.GetEditCost();

        public override bool IsDirty =>
            (RWrapper.IsAppearanceDirty ? 1 : 0) +
            (LWrapper.IsAppearanceDirty ? 1 : 0)
            > 0;
    
    
        public override void ApplyChanges()
        {
            RWrapper.MarkAsOriginal();
            LWrapper.MarkAsOriginal();
            OnDirty?.Invoke();
        }

        public override void RevertChanges()
        {
            RWrapper.RestoreOriginal();
            LWrapper.RestoreOriginal();
            MainSelection = _mainSelection;
            OnDirty?.Invoke();
        }


        void OnItemEquip(OnItemEquip data)
        {
            if(data.PreventEquip || !data.EquippedBy.IsValid) return;

            var slot = data.Slot;
            var item = data.Item;

            if(item == null || !item.IsValid || !item.TryGetUUID(out var guid))
                return;

            WeaponItemWrapper wrapper;
            switch (slot)
            {
                case InventorySlot.RightHand:
                    wrapper = RWrapper;
                    break;
                case InventorySlot.LeftHand:
                    wrapper = LWrapper;
                    break;

                default: return;
            }

            if(wrapper.IsReEquippingAfterAppearanceChange) return;

            if (wrapper.HasItem)
            {
                wrapper.RestoreOriginal();
                wrapper.ClearItem();
            }

            wrapper.Item = item;
            wrapper.OriginalGuid = guid;
            wrapper.CacheCurrentAppearance();
            wrapper.MarkAsOriginal();

            if (LeftSide)
            {
                if(!LWrapper.HasItem || !LWrapper.IsShield || !LWrapper.IsSupportedItemType)
                    _leftSide = false;
            }
            else if(!RWrapper.HasItem && LWrapper.HasItem && LWrapper.IsShield && LWrapper.IsSupportedItemType)
                _leftSide = true;

            RefreshMainEntries();
            MainSelection = _mainSelection;

            OnDirty?.Invoke();

        }

        void OnItemUnequip(OnItemUnequip data)
        {
            var item = data.Item;

            if(data.PreventUnequip || !data.Creature.IsValid || !item.IsValid)
                return;
            

            else if(LWrapper.HasItem && !LWrapper.IsReEquippingAfterAppearanceChange && LWrapper.Item == item)
            {
                LWrapper.RestoreOriginal();
                LWrapper.ClearItem();
                
                if(LeftSide)
                {
                    if(RWrapper.HasItem && RWrapper.IsSupportedItemType)
                        _leftSide = false;
                }
            }
            
            else if(RWrapper.HasItem && !RWrapper.IsReEquippingAfterAppearanceChange && RWrapper.Item == item)
            {
                RWrapper.RestoreOriginal();
                RWrapper.ClearItem();

                if(!LeftSide)
                {
                    if(LWrapper.HasItem && LWrapper.IsShield && LWrapper.IsSupportedItemType)
                        _leftSide = true;
                }
            }

            else return;

            RefreshMainEntries();
            MainSelection = _mainSelection;
            OnDirty?.Invoke();
        }
    }
}