using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using CharacterAppearance.Wrappers;
using ExtensionsPlugin;

namespace CharacterAppearance.UI
{
    internal sealed class ArmorEditorModel : AppearanceEditorModel, IDisposable
    {
        private const int CloakPartIndex = -99;        
        private static readonly string[] _creaturePartNames = new string[]
        {
            "Nakrycie głowy",
            "Tors",
            "Barki",
            "Ramiona",
            "Przedramiona",
            "Dłonie",
            "Pas",
            "Biodra",
            "Uda",
            "Łydki",
            "Stopy",
            "Szata",
            "Dodatki",
            "Płaszcz"
        };

        private static readonly int[] _allKeys = new int[]
        {            
            (int)CreaturePart.Head,
            (int)CreaturePart.Torso,
            (int)CreaturePart.RightShoulder,
            (int)CreaturePart.RightBicep,
            (int)CreaturePart.RightForearm,
            (int)CreaturePart.RightHand,
            (int)CreaturePart.Belt,
            (int)CreaturePart.Pelvis,
            (int)CreaturePart.RightThigh,
            (int)CreaturePart.RightShin,
            (int)CreaturePart.RightFoot,
            (int)CreaturePart.Robe,
            (int)CreaturePart.Neck,
            CloakPartIndex
        };

        private static readonly int[] _armorParts = _allKeys.Except(new int[]{(int)CreaturePart.Head, CloakPartIndex}).ToArray();

        private static readonly int[] _satyrArmorParts = _armorParts.Except(new int[]{
            (int)CreaturePart.LeftThigh,
            (int)CreaturePart.RightThigh,
            (int)CreaturePart.LeftShin,
            (int)CreaturePart.RightShin,
            (int)CreaturePart.LeftFoot,
            (int)CreaturePart.RightFoot,
            (int)CreaturePart.Pelvis
        }).ToArray();


        private int[] _availableKeys;
        private readonly Dictionary<int, IReadOnlyList<int>> _availableParts;

        public override int AppearanceChangeCost => (HelmetItem?.GetEditCost() ?? 0) + (CloakItem?.GetEditCost() ?? 0) + (ArmorItem?.GetEditCost() ?? 0);

        private readonly NwCreature _pc;

        readonly ArmorItemWrapper CloakItem;
        readonly ArmorItemWrapper ArmorItem;
        readonly ArmorItemWrapper HelmetItem;

        private int _mainSelection = 0;
        public override int MainSelection
        {
            get => _mainSelection; 
            set
            {
                if(_availableKeys.Length == 0)
                {
                    _mainSelection = 0;
                    return;
                }

                var val = Math.Max(0,Math.Min(value,_availableKeys.Length - 1));

                _mainSelection = val;

                if (!IsDoublePart) _leftSide = false;

                RefreshSubEntries();

                SubSelection = GetCurrentSubSelection();
            }
        }

        private int _subSelection = 0;
        public override int SubSelection
        {
            get => _subSelection;
            set
            {
                if(_subSelection == value || _availableParts.Count == 0 || CurrentItem == null || !(CurrentItem.HasItem && CurrentItem.IsSupportedItem)) return;

                var selectedPart = SelectedPart;

                var parts = _availableParts[selectedPart];

                if(value < 0 || value >= parts.Count) return;

                _subSelection = value;
                
                var model = parts[value];

                if (selectedPart == CloakPartIndex && CloakItem.HasItem && CloakItem.IsSupportedItem) 
                    CloakItem.SetModel(model);
                    
                else if (selectedPart == (int)CreaturePart.Head && HelmetItem.HasItem && HelmetItem.IsSupportedItem) 
                    HelmetItem.SetModel(model);
                    
                else if(ArmorItem.HasItem && ArmorItem.IsSupportedItem)
                {
                    var key = selectedPart;
                    if(LeftSide) key = (int)GetOppositePart((CreaturePart)key);

                    ArmorItem.SetModel(model, key);
                }
            }
        }

        
        int SelectedPart
        {
            get
            {
                if(_mainSelection < 0 || _mainSelection >= _availableKeys.Length)
                    return 0;

                return _availableKeys[_mainSelection];
            }
        }

        List<string> _mainEntries = new();
        public override IEnumerable<string> MainEntries => _mainEntries;
        public override int MainEntriesCount => _mainEntries.Count;

        List<string> _subEntries = new();
        public override IEnumerable<string> SubEntries => _subEntries;
        public override int SubEntriesCount => _subEntries.Count;



        private bool _leftSide = false;
        public override bool LeftSide
        {
            get => _leftSide;
            set
            {
                if(_leftSide == value) return;

                _leftSide = value;
                RefreshSubEntries();
                SubSelection = GetCurrentSubSelection();
            }
        }


        public override bool IsSymmetrical
        {
            get
            {
                if(SelectedPart == -1 || SelectedPart == CloakPartIndex || SelectedPart == (int)CreaturePart.Head || !ArmorItem.HasItem) return true;

                var key = SelectedPart;

                var oppKey = (int)GetOppositePart((CreaturePart)key);

                if(key == oppKey) return true;

                return ArmorItem.CurrentAppearance[key].SequenceEqual(ArmorItem.CurrentAppearance[oppKey]);
            }
        }

        public override bool IsDoublePart => SelectedPart != CloakPartIndex && (int)GetOppositePart((CreaturePart)SelectedPart) != SelectedPart;


        private int _selectedColorChannel = -1;
        public override int SelectedColorChannel
        {
            get => _selectedColorChannel;
            set
            {
                if(value != _selectedColorChannel && value >= 0 && _mainSelection >= 0 && _mainSelection < _availableParts.Count){
                    _selectedColorChannel = value;
                }
                else _selectedColorChannel = -1;
            }
        }

        public override int SelectedColorIndex
        {
            get
            {
                if(_selectedColorChannel < 0 || _selectedColorChannel > 5 || _mainSelection < 0 || _mainSelection >= MainEntriesCount) return 255;

                if(SelectedPart == CloakPartIndex && CloakItem.HasItem)
                {
                    return CloakItem.CurrentAppearance[CloakPartIndex][ApprTypeColorChannel + 1];
                }
                else if(SelectedPart == (int)CreaturePart.Head && HelmetItem.HasItem)
                {
                    return HelmetItem.CurrentAppearance[(int)CreaturePart.Head][ApprTypeColorChannel + 1];
                }
                else if(ArmorItem.HasItem)
                {
                    int key = SelectedPart;
                    if(LeftSide) key = (int)GetOppositePart((CreaturePart)key);

                    return ArmorItem.CurrentAppearance[key][ApprTypeColorChannel + 1];
                }
                else return 255;
            }
            set
            {
                if(_selectedColorChannel < 0 || _selectedColorChannel > 5) return;

                int col = 255;
                var channel = (ItemAppearanceArmorColor)ApprTypeColorChannel;

                if(value >= 0 && (value == 255 || value <= 175))
                    col = SelectedColorIndex == value ? 255 : value;

                if(_mainSelection < 0 || _mainSelection >= MainEntriesCount) return;

                if(SelectedPart == CloakPartIndex && CloakItem.HasItem)
                    CloakItem.SetColor(col, channel);
                
                else if(SelectedPart == (int)CreaturePart.Head && HelmetItem.HasItem)
                    HelmetItem.SetColor(col, channel);
                    
                else if(ArmorItem.HasItem)
                {
                    int key = SelectedPart;
                    if (LeftSide) key = (int)GetOppositePart((CreaturePart)key);

                    ArmorItem.SetColor(col, channel, key);
                }
            }
        }

        public event System.Action? OnDirty;

        ArmorItemWrapper? CurrentItem => MainSelection < 0 || MainSelection >= MainEntriesCount ? null : 
            SelectedPart switch
            {
                CloakPartIndex => CloakItem,
                (int)CreaturePart.Head => HelmetItem,
                _ => ArmorItem
            };

        private int ApprTypeColorChannel => GetApprTypeColorChannel(_selectedColorChannel);

        public override ColorChart CurrentColorChart => _selectedColorChannel switch
        {
            0 or 1 => ColorChart.Cloth,
            2 or 3 => ColorChart.Leather,
            4 or 5 => ColorChart.Metal,
            _ => ColorChart.Skin  
        };


        private void ResolveAvailableKeys()
        {
            bool isSatyr = _pc.IsSatyr();
            int len = 
                ((!(HelmetItem.HasItem && HelmetItem.IsSupportedItem)) ? 0 : 1) + 
                ((!(CloakItem.HasItem && CloakItem.IsSupportedItem)) ? 0 : 1) +
                ((!(ArmorItem.HasItem && ArmorItem.IsSupportedItem)) ? 0 : (isSatyr ? _satyrArmorParts.Length : _armorParts.Length));


            if(len == 0)
            {
                _availableKeys = Array.Empty<int>();
                return;
            }

            _availableKeys = new int[len];

            if(HelmetItem.HasItem && HelmetItem.IsSupportedItem) _availableKeys[0] = (int)CreaturePart.Head;
            if(CloakItem.HasItem && CloakItem.IsSupportedItem) _availableKeys[^1] = CloakPartIndex;
            if(ArmorItem.HasItem && ArmorItem.IsSupportedItem)
            {
                int startIndex = !HelmetItem.HasItem ? 0 : 1;
                if(isSatyr) _satyrArmorParts.CopyTo(_availableKeys, startIndex);
                else _armorParts.CopyTo(_availableKeys, startIndex);
            }
        }
        
        private void RefreshMainEntries()
        {
            if(_availableKeys.Length == 0) _mainEntries.Clear();
            else _mainEntries = _creaturePartNames.Where((s,i) => _availableKeys.Contains(_allKeys[i])).ToList();
        }
        private void RefreshSubEntries() 
        {
            var selectedPart = SelectedPart;
            if(_availableKeys.Length == 0 || CurrentItem == null || !(CurrentItem.HasItem && CurrentItem.IsSupportedItem)) _subEntries.Clear();
            else if(selectedPart == (int)CreaturePart.Torso) _subEntries = AvailableItems.GetAvailableTorsoParts(CurrentItem.Item, _pc.Gender).Select(i=>i.ToString()).ToList();
            else _subEntries = AvailableItems.GetAvailableItemParts(selectedPart, _pc.Gender).Select(i=>i.ToString()).ToList();
        }
        private int GetCurrentSubSelection() => SelectedPart switch
        {
            CloakPartIndex => !(CloakItem.HasItem && CloakItem.IsSupportedItem) ? 0 : _subEntries.IndexOf(CloakItem.CurrentAppearance[CloakPartIndex][0].ToString()),
            (int)CreaturePart.Head => !(HelmetItem.HasItem && HelmetItem.IsSupportedItem) ? 0 : _subEntries.IndexOf(HelmetItem.CurrentAppearance[(int)CreaturePart.Head][0].ToString()),
            _ => !(ArmorItem.HasItem && ArmorItem.IsSupportedItem) ? 0 : _subEntries.IndexOf(ArmorItem.CurrentAppearance[LeftSide ? (int)GetOppositePart((CreaturePart)SelectedPart) : SelectedPart][0].ToString())
        };

        private readonly EditorFlags _flags;
        public ArmorEditorModel(NwCreature pc, EditorFlags flags)
        {
            _pc = pc;
            _flags = flags;

            var player = pc.ControllingPlayer;

            if(player == null || !player.IsValid) throw new InvalidOperationException("No player controlling this creature");

            CharacterAppearanceService.EventService.Unsubscribe<OnItemEquip, OnItemEquip.Factory>(_pc, OnItemEquip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Unsubscribe<OnItemUnequip, OnItemUnequip.Factory>(_pc, OnItemUnequip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Subscribe<OnItemEquip, OnItemEquip.Factory>(_pc, OnItemEquip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Subscribe<OnItemUnequip, OnItemUnequip.Factory>(_pc, OnItemUnequip, Anvil.Services.EventCallbackType.After);

            _availableParts = new();
            _availableKeys = Array.Empty<int>();

            HelmetItem = new(_flags);
            ArmorItem = new(_flags);
            CloakItem = new(_flags);

            var helmet = _pc.GetItemInSlot(InventorySlot.Head);
            if (helmet != null && helmet.IsValid && helmet.TryGetUUID(out Guid guid) && guid != Guid.Empty){
                HelmetItem.Item = helmet;
                if(HelmetItem.HasItem && HelmetItem.IsSupportedItem)
                {
                    HelmetItem.OriginalGuid = guid;
                    HelmetItem.CacheCurrentAppearance();
                    HelmetItem.MarkAsOriginal();
                    SetupHelmetParts();
                }
                else HelmetItem.ClearItem();
            }

            var armor = _pc.GetItemInSlot(InventorySlot.Chest);
            if (armor != null && armor.IsValid && armor.TryGetUUID(out guid) && guid != Guid.Empty)
            {
                ArmorItem.Item = armor;
                if(ArmorItem.HasItem && ArmorItem.IsSupportedItem)
                {
                    ArmorItem.OriginalGuid = guid;
                    ArmorItem.CacheCurrentAppearance();
                    ArmorItem.MarkAsOriginal();
                    SetupArmorParts();
                }
                else ArmorItem.ClearItem();
            }

            var cloak = _pc.GetItemInSlot(InventorySlot.Cloak);
            if (cloak != null && cloak.IsValid && cloak.TryGetUUID(out guid) && guid != Guid.Empty)
            {
                CloakItem.Item = cloak;
                if(CloakItem.HasItem && CloakItem.IsSupportedItem)
                {
                    CloakItem.OriginalGuid = guid;
                    CloakItem.CacheCurrentAppearance();
                    CloakItem.MarkAsOriginal();
                    SetupCloakParts();
                }
                else CloakItem.ClearItem();
            }

            ResolveAvailableKeys();

            RefreshMainEntries();
            
            MainSelection = 0;
        }

        public void Dispose()
        {
            if (!_pc.IsValid) return;
            
            CharacterAppearanceService.EventService.Unsubscribe<OnItemEquip, OnItemEquip.Factory>(_pc, OnItemEquip, Anvil.Services.EventCallbackType.After);
            CharacterAppearanceService.EventService.Unsubscribe<OnItemUnequip, OnItemUnequip.Factory>(_pc, OnItemUnequip, Anvil.Services.EventCallbackType.After);
        }

        bool OnHelmetItemChanged(NwItem? item)
        {
            if(HelmetItem.IsReEquippingAfterAppearanceChange)
                return false;

            if(HelmetItem.HasItem)
                HelmetItem.RestoreOriginal();

            if(item == null || !item.IsValid || !item.TryGetUUID(out var guid) || guid == Guid.Empty)
            {
                HelmetItem.ClearItem();
                return true;
            }

            HelmetItem.Item = item;
            if(HelmetItem.HasItem && HelmetItem.IsSupportedItem)
            {
                HelmetItem.OriginalGuid = guid;
                HelmetItem.CacheCurrentAppearance();
                HelmetItem.MarkAsOriginal();
                SetupHelmetParts();
            }
            else HelmetItem.ClearItem();

            return true;
        }

        void SetupHelmetParts()
        {
            var availableModels = AvailableItems.GetAvailableItemParts((int)CreaturePart.Head, _pc.Gender);

            if(!_availableParts.TryAdd((int)CreaturePart.Head, availableModels))
                _availableParts[(int)CreaturePart.Head] = availableModels;

        }

        bool OnArmorItemChanged(NwItem? item)
        {
            if(ArmorItem.IsReEquippingAfterAppearanceChange)
                return false;

            if(ArmorItem.HasItem)
                ArmorItem.RestoreOriginal();

            if(item == null || !item.IsValid || !item.TryGetUUID(out var guid) || guid == Guid.Empty)
            {
                ArmorItem.ClearItem();
                return true;
            }

            ArmorItem.Item = item;
            if(ArmorItem.HasItem && ArmorItem.IsSupportedItem)
            {
                ArmorItem.OriginalGuid = guid;
                ArmorItem.CacheCurrentAppearance();
                ArmorItem.MarkAsOriginal();
                SetupArmorParts();
            }
            else ArmorItem.ClearItem();

            return true;
        }
        void SetupArmorParts()
        {
            IReadOnlyList<int> availableModels;

            int[] validArmorParts = _pc.IsSatyr() ? _satyrArmorParts : _armorParts;

            Gender gender = _pc.Gender;

            foreach(var k in validArmorParts)
            {
                availableModels = k == (int)CreaturePart.Torso 
                    ? AvailableItems.GetAvailableTorsoParts(ArmorItem.Item, gender) 
                    : AvailableItems.GetAvailableItemParts(k, gender);
                
                if(!_availableParts.TryAdd(k, availableModels))
                    _availableParts[k] = availableModels;
            }
        }

        bool OnCloakItemChanged(NwItem? item)
        {
            if(CloakItem.IsReEquippingAfterAppearanceChange)
                return false;

            if(CloakItem.HasItem)
                CloakItem.RestoreOriginal();

            if(item == null || !item.IsValid || !item.TryGetUUID(out var guid) || guid == Guid.Empty)
            {
                CloakItem.ClearItem();
                return true;
            }

            CloakItem.Item = item;
            if(CloakItem.HasItem && CloakItem.IsSupportedItem)
            {
                CloakItem.OriginalGuid = guid;
                CloakItem.CacheCurrentAppearance();
                CloakItem.MarkAsOriginal();
                SetupCloakParts();
            }
            else CloakItem.ClearItem();

            return true;
        }
        void SetupCloakParts()
        {
            var availableModels = AvailableItems.GetAvailableItemParts(CloakPartIndex, _pc.Gender);

            if(!_availableParts.TryAdd(CloakPartIndex, availableModels))
                _availableParts[CloakPartIndex] = availableModels;
        }

        void OnItemEquip(OnItemEquip data)
        {        
            if(!data.EquippedBy.IsValid || data.PreventEquip) return;

            var slot = data.Slot;

            switch (slot)
            {
                case InventorySlot.Head:
                case InventorySlot.Chest:
                case InventorySlot.Cloak:
                    break;
                default: return;
            }

            var item = data.Item;        

            if(!item.IsValid || !item.TryGetUUID(out var guid) || guid == Guid.Empty)
                return;

            var bit = item.BaseItem.ItemType;
            int newMainSelection = 0;

            switch (bit)
            {
                case BaseItemType.Helmet:
                    if(ArmorItem.HasItem || CloakItem.HasItem)
                        newMainSelection = _mainSelection + 1;
                break;
                case BaseItemType.Armor:
                    if(CloakItem.HasItem && _mainSelection == _availableKeys.Length - 1)
                        newMainSelection = -1;
                break;
                case BaseItemType.Cloak:
                    newMainSelection = _mainSelection;
                    break;
                default: return;
            }

            bool changeHandled = slot switch
            {
                InventorySlot.Head => OnHelmetItemChanged(item),
                InventorySlot.Chest => OnArmorItemChanged(item),
                InventorySlot.Cloak => OnCloakItemChanged(item),
                _ => false
            };

            if (!changeHandled)
            {
                MainSelection = _mainSelection;
                //OnDirty?.Invoke();
                return;
            }
            
            ResolveAvailableKeys();

            RefreshMainEntries();
            
            if(newMainSelection == -1)
                newMainSelection = _availableKeys.Length - 1;

            MainSelection = newMainSelection;

            OnDirty?.Invoke();

        }

        void OnItemUnequip(OnItemUnequip data)
        {
            var item = data.Item;

            if (data.PreventUnequip || !item.IsValid || !data.Creature.IsValid)
                return;

            int newMainSelection = 0;

            ItemWrapper wrapper;

            if(item.BaseItem.ItemType == BaseItemType.Armor)
            {
                if(ArmorItem.IsReEquippingAfterAppearanceChange) return;

                if(CloakItem.HasItem && _mainSelection == _availableKeys.Length - 1 && HelmetItem.HasItem)
                    newMainSelection = 1;

                wrapper = ArmorItem;
            }
            else if(item.BaseItem.ItemType == BaseItemType.Helmet)
            {
                if(HelmetItem.IsReEquippingAfterAppearanceChange) return;

                if(_mainSelection > 0) 
                    newMainSelection = _mainSelection - 1;

                wrapper = HelmetItem;
            }
            else if(item.BaseItem.ItemType == BaseItemType.Cloak)
            {
                if(CloakItem.IsReEquippingAfterAppearanceChange) return;

                if(_mainSelection == _availableKeys.Length - 1 && (HelmetItem.HasItem || ArmorItem.HasItem))
                    newMainSelection = _mainSelection - 1; 
                    
                wrapper = CloakItem;
            }
            else return;

            if (wrapper.HasItem)
            {
                wrapper.RestoreOriginal();
                wrapper.ClearItem();
            }
            
            ResolveAvailableKeys();

            RefreshMainEntries();

            MainSelection = newMainSelection;

            RefreshSubEntries();

            OnDirty?.Invoke();
        }
        public override void RevertChanges()
        {
            HelmetItem.RestoreOriginal();
            ArmorItem.RestoreOriginal();
            CloakItem.RestoreOriginal();
            SubSelection = GetCurrentSubSelection();
        }

        public override void CopyToTheOtherSide()
        {
            if(!ArmorItem.HasItem || !ArmorItem.IsSupportedItem || !IsDoublePart || IsSymmetrical || _mainSelection < 0 || _mainSelection >= MainEntriesCount) return;

            CreaturePart fromPart = LeftSide ? GetOppositePart((CreaturePart)SelectedPart) : (CreaturePart)SelectedPart;
            CreaturePart toPart = GetOppositePart(fromPart);

            int[] values = ArmorItem.CurrentAppearance[(int)fromPart];

            ArmorItem.SetModelWithColors(values, (int)toPart);

            SubSelection = GetCurrentSubSelection();
        }

        public override bool IsValidColor(int colorID) => true;
        
        private static int GetApprTypeColorChannel(int index) => index switch
        {
            0 => (int)ItemAppearanceArmorColor.Cloth1,
            1 => (int)ItemAppearanceArmorColor.Cloth2,
            2=> (int)ItemAppearanceArmorColor.Leather1,
            3=>(int)ItemAppearanceArmorColor.Leather2,
            4=>(int)ItemAppearanceArmorColor.Metal1,
            _=>(int)ItemAppearanceArmorColor.Metal2
        };


        public override bool IsDirty => 
        (HelmetItem.IsAppearanceDirty ? 1 : 0) + 
        (ArmorItem.IsAppearanceDirty ? 1 : 0) + 
        (CloakItem.IsAppearanceDirty ? 1 : 0) 
        > 0;

        public override void ApplyChanges()
        {            
            if(HelmetItem.IsAppearanceDirty) HelmetItem.MarkAsOriginal();
            if(ArmorItem.IsAppearanceDirty) ArmorItem.MarkAsOriginal();
            if(CloakItem.IsAppearanceDirty) CloakItem.MarkAsOriginal();
        }
    }
}