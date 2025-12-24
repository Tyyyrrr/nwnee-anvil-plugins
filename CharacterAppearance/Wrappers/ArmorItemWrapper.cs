using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace CharacterAppearance.Wrappers
{
    internal sealed class ArmorItemWrapper : ItemWrapper
    {
        private static readonly ItemAppearanceArmorColor[] _colorChannels = Enum.GetValues<ItemAppearanceArmorColor>().ToArray();
        private static readonly Dictionary<int, int[]> _emptyOriginalAppearance = new();
        private static readonly Dictionary<int, int[]> _emptyAppearance = new();

        private bool _isDirty = false;
        public bool IsAppearanceDirty => HasItem && _isDirty;

        public bool IsSupportedItem {get;private set;}


        private Dictionary<int, int[]> _originalAppearance = _emptyOriginalAppearance;
        public Dictionary<int, int[]> CurrentAppearance {get;private set;} = _emptyAppearance;

        public bool IsReEquippingAfterAppearanceChange {get;private set;} = false;

        private readonly EditorFlags _flags;
        public ArmorItemWrapper(EditorFlags flags)
        {
            _flags = flags;
            IsSupportedItem = false;
        }

        bool IsItemSupported(NwItem item)
        {
            if(!item.IsValid) 
                return false;

            var bit = item.BaseItem.ItemType;

            if(bit == BaseItemType.Cloak || bit == BaseItemType.Helmet) 
                return true;

            else if(bit != BaseItemType.Armor) 
                return false;

            var ac = item.BaseACValue;
            if(ac > 4) return _flags.HasFlag(EditorFlags.ArmorHeavy);
            if(ac > 0) return _flags.HasFlag(EditorFlags.ArmorMedium);
            return _flags.HasFlag(EditorFlags.ArmorLight); 
        }

        public override void RestoreOriginal()
        {
            if(!HasItem || IsReEquippingAfterAppearanceChange)
                return;

            if(IsAppearanceDirty) RestoreOriginalAppearance();
            else if(IsGuidDirty) RestoreOriginalUUID();
        }

        public void CacheCurrentAppearance()
        {
            if(!HasItem) return;

            var bit = Item.BaseItem.ItemType;

            if(CurrentAppearance == _emptyAppearance) CurrentAppearance = new();
            else CurrentAppearance.Clear();
            
            switch(bit)
            {
                case BaseItemType.Armor:
                {
                    foreach(var cp in Enum.GetValues<CreaturePart>())
                    {
                        if(cp == CreaturePart.Head) continue;

                        var values = new int[_colorChannels.Length + 1];
                        values[0] = Item.Appearance.GetArmorModel(cp);
                        foreach(var armCol in _colorChannels){
                            var val = Item.Appearance.GetArmorPieceColor(cp, armCol);
                            values[(int)armCol + 1] = val;
                        }

                        CurrentAppearance.Add((int)cp, values);
                    }
                }
                break;

                case BaseItemType.Helmet:
                case BaseItemType.Cloak:
                {
                    var values = new int[_colorChannels.Length + 1];
                    values[0] = Item.Appearance.GetSimpleModel();
                    foreach(var armCol in _colorChannels){
                        var val = Item.Appearance.GetArmorColor(armCol);
                        values[(int)armCol + 1] = val;
                    }
                    int key = bit == BaseItemType.Cloak ? -99 : (int)CreaturePart.Head;

                    CurrentAppearance.Add(key, values);
                }
                break;

                default: 
                    ClearItem(); 
                    break;
            }
        }

        public override void MarkAsOriginal()
        {
            base.MarkAsOriginal();

            if (!HasItem || CurrentAppearance == _emptyAppearance || CurrentAppearance.Count == 0)
            {
                ClearItem();
                return;
            }

            if(_originalAppearance == _emptyOriginalAppearance)
                _originalAppearance = new();

            foreach(var kvp in CurrentAppearance)
            {
                var arr = new int[kvp.Value.Length];

                kvp.Value.CopyTo(arr,0);

                if(!_originalAppearance.TryAdd(kvp.Key, arr))
                    _originalAppearance[kvp.Key] = arr;
            }

            _isDirty = false;
        }

        public override void ClearItem()
        {
            CurrentAppearance = _emptyAppearance;
            _originalAppearance = _emptyOriginalAppearance;
            _isDirty = false;
            IsSupportedItem = false;
            base.ClearItem();
        }

        public override NwItem Item 
        { 
            get => base.Item; 
            set 
            {
                if(HasItem && base.Item == value && IsSupportedItem)
                    return;

                base.Item = value;

                IsSupportedItem = IsItemSupported(value);

                if (!HasItem || !IsSupportedItem) ClearItem();
            }
        }

        public void RestoreOriginalAppearance()
        {
            var editableParts = _originalAppearance.Count;
            
            if(!HasItem || !IsAppearanceDirty || editableParts < 1) return;


            IsReEquippingAfterAppearanceChange = true;

            if(editableParts > 1)
            {
                Item = Item.Appearance.ChangeAppearance((a) =>
                {
                    foreach(var kvp in _originalAppearance)
                    {
                        if(!Enum.IsDefined(typeof(CreaturePart), kvp.Key) || !CurrentAppearance.TryGetValue(kvp.Key, out var ints) || ints.SequenceEqual(kvp.Value))
                            continue;

                        var part = (CreaturePart)kvp.Key;

                        var model = kvp.Value[0];
                        if(model >= 0 && model <= ushort.MaxValue && model != ints[0])
                            a.SetArmorModel(part, (ushort)model);

                        for(int i = 1; i < ints.Length; i++)
                        {
                            var channel = (ItemAppearanceArmorColor)(i-1);
                            var colId = kvp.Value[i];
                            if(colId != ints[i] && colId >= 0 && colId <= byte.MaxValue)
                                a.SetArmorPieceColor(part,channel,(byte)colId);
                        }
                    }
                });
            }
            else
            {
                Item = Item.Appearance.ChangeAppearance((a)=>
                {
                    var kvp = _originalAppearance.First();
                    
                    if(!CurrentAppearance.TryGetValue(kvp.Key, out var ints) || ints.SequenceEqual(kvp.Value))
                        return;

                    var model = kvp.Value[0];

                    if(model >= 0 && model <= ushort.MaxValue && model != ints[0])
                        a.SetSimpleModel((ushort)model);

                    for(int i = 1; i < kvp.Value.Length; i++)
                    {
                        var channel = (ItemAppearanceArmorColor)(i-1);
                        var colId = kvp.Value[i];

                        if(colId != ints[i] && colId >= 0 && colId <= byte.MaxValue)
                            a.SetArmorColor(channel, (byte)colId);
                    }
                });
                
            }

            IsReEquippingAfterAppearanceChange = false;

            foreach(var kvp in _originalAppearance) kvp.Value.CopyTo(CurrentAppearance[kvp.Key],0);
            
            RestoreOriginalUUID();

            _isDirty = false;
        }

        public void SetModel(int model, int part = -1)
        {
            if(!HasItem) return;

            if(model < 0 || model > ushort.MaxValue || CurrentAppearance.Count == 0 || (CurrentAppearance.Count > 1 && !Enum.IsDefined(typeof(CreaturePart), part)))
                return;
            
            int partIndex = part < 0 ? CurrentAppearance.Keys.First() : part;

            if(CurrentAppearance[partIndex][0] == model) return;
            
            int oldModel = CurrentAppearance[partIndex][0];
            int originalModelBefore = _originalAppearance[partIndex][0];

            CurrentAppearance[partIndex][0] = model;              
            int originalModel = _originalAppearance[partIndex][0];  

            if(!_isDirty)
            {
                _isDirty = model != originalModel;
            }
            else
            {
                _isDirty = false;
                foreach(var kvp in _originalAppearance)
                {
                    if(CurrentAppearance.TryGetValue(kvp.Key, out var ints) && !ints.SequenceEqual(kvp.Value))
                    {
                        _isDirty = true;
                        break;
                    }
                }
            }

            IsReEquippingAfterAppearanceChange = true;
            if(CurrentAppearance.Count == 1) Item = Item.Appearance.ChangeAppearance((a)=>a.SetSimpleModel((ushort)model));
            else Item = Item.Appearance.ChangeAppearance((a)=>a.SetArmorModel((CreaturePart) part, (ushort)model));
            IsReEquippingAfterAppearanceChange = false;

            RestoreOriginalUUID();
        }

        public void SetColor(int color, ItemAppearanceArmorColor channel, int part = -1)
        {
            if(!HasItem) return;

            part = part < 0 ? CurrentAppearance.Keys.First() : part;

            if(color < 0 || (color >= 11*16 && color != 255) || CurrentAppearance.Count == 0 || (CurrentAppearance.Count > 1 && !Enum.IsDefined(typeof(CreaturePart), part)))
                return;

            else if(CurrentAppearance[part][(int)channel + 1] == color) 
                return;

            CurrentAppearance[part][(int)channel + 1] = color;
            int originalColor = _originalAppearance[part][(int)channel + 1];

            if(!_isDirty)
            {
                _isDirty = color != originalColor;
            }
            else
            {
                _isDirty = false;
                foreach(var kvp in _originalAppearance)
                {
                    if(CurrentAppearance.TryGetValue(kvp.Key, out var ints) && !ints.SequenceEqual(kvp.Value))
                    {
                        _isDirty = true;
                        break;
                    }
                }
            }
            IsReEquippingAfterAppearanceChange = true;
            if(CurrentAppearance.Count == 1) Item = Item.Appearance.ChangeAppearance(a=>a.SetArmorColor(channel, (byte)color));
            else Item = Item.Appearance.ChangeAppearance((a)=>a.SetArmorPieceColor((CreaturePart)part, channel, (byte)color));
            IsReEquippingAfterAppearanceChange = false;

            RestoreOriginalUUID();
        }

        public void SetModelWithColors(int[] array, int part = -1)
        {
            if(!HasItem) return;

            if(array.Length != 7) throw new InvalidOperationException("Array length has to be 7");
            if(CurrentAppearance.Count == 0) throw new InvalidOperationException("No current appearance keys");
            if(CurrentAppearance.Count > 1 && !Enum.IsDefined(typeof(CreaturePart), part)) throw new InvalidOperationException("Enum is not defined");
            if(array[0]<0) throw new InvalidOperationException("Array[0] value is less than zero");
            if(array[0] > ushort.MaxValue) throw new InvalidOperationException("Array[0] value is greater than max ushort value");
            if(array[1..^1].Any(v=>v<0||v>byte.MaxValue)) throw new InvalidOperationException("color value out of byte range");

            if(array.Length != 7 || CurrentAppearance.Count == 0 || (CurrentAppearance.Count > 1 && !Enum.IsDefined(typeof(CreaturePart), part))) return;
            if(array[0] < 0 || array[0] > ushort.MaxValue || array[1..^1].Any(v=>v < 0 || v > byte.MaxValue)) return;

            var key = part < 0 ? CurrentAppearance.Keys.First() : part;

            if(CurrentAppearance[key].SequenceEqual(array)) return;

            array.CopyTo(CurrentAppearance[key],0);

            int[] oldArray = _originalAppearance[key];

            if(!_isDirty)
            {
                _isDirty = !oldArray.SequenceEqual(array);
            }
            else
            {
                _isDirty = false;
                foreach(var kvp in _originalAppearance)
                {
                    if(CurrentAppearance.TryGetValue(kvp.Key, out var ints) && !ints.SequenceEqual(kvp.Value))
                    {
                        _isDirty = true;
                        break;
                    }
                }
            }

            IsReEquippingAfterAppearanceChange = true;
            if(CurrentAppearance.Count == 1)
            {
                Item = Item.Appearance.ChangeAppearance((a) =>
                {
                    if(array[0] != oldArray[0]) a.SetSimpleModel((ushort)array[0]);

                    for(int i = 1; i < array.Length; i++)
                    {
                        if(array[i] == oldArray[i])
                            continue;

                        var channel = (ItemAppearanceArmorColor)(i - 1);
                        a.SetArmorColor(channel,(byte)array[i]);
                    }
                });
            }
            else
            {
                Item = Item.Appearance.ChangeAppearance((a) =>
                {
                    if(array[0] != oldArray[0]) a.SetArmorModel((CreaturePart)part, (ushort)array[0]);

                    for(int i = 1; i < array.Length; i++)
                    {
                        if(array[i] == oldArray[i])
                            continue;

                        var channel = (ItemAppearanceArmorColor)(i-1);
                        var col = array[i];

                        a.SetArmorPieceColor((CreaturePart)part, channel, (byte)col);
                    }
                });
            }
            IsReEquippingAfterAppearanceChange = false;

            RestoreOriginalUUID();
            
        }
                
        
        public int GetEditCost()
        {
            if(!HasItem || !IsAppearanceDirty) return 0;

            (float,float) minMax = CharacterAppearanceService.ArmorEditCostMultiplierMinMax;

            int gpVal = Item.GoldValue;
            var minMaxCost = (gpVal * minMax.Item1, gpVal * minMax.Item2);
            float colorToPartRatio = CharacterAppearanceService.ArmorEditColorToPartRatio;
            int editableParts = CurrentAppearance.Count;
            var costPerPart = minMaxCost.Item2 / editableParts;
            var costPerColorChange = costPerPart * colorToPartRatio;
            var costPerModelChange = costPerPart - costPerColorChange;

            int colorsChanged = 0;
            int modelsChanged = 0;
            float colorsCost, modelsCost;

            int total;

            if (editableParts == 1) // cloaks and helments
            {
                var kvp = _originalAppearance.First();
                if (kvp.Key != -99 && kvp.Key != (int)CreaturePart.Head)
                    return -1; // invalid key

                if (kvp.Value[0] != CurrentAppearance[kvp.Key][0])
                    modelsChanged = 1;
                
                foreach (var armCol in _colorChannels)
                {
                    if (kvp.Value[(int)armCol + 1] != CurrentAppearance[kvp.Key][(int)armCol + 1])
                    {
                        colorsChanged = 1;
                        break;
                    }
                }
            }
            else
            {
                foreach (var kvp in _originalAppearance)
                {
                    if (kvp.Value[0] != CurrentAppearance[kvp.Key][0])
                        modelsChanged++;

                    foreach (var armCol in _colorChannels)
                    {
                        if (kvp.Value[(int)armCol + 1] != CurrentAppearance[kvp.Key][(int)armCol + 1])
                        {
                            colorsChanged++;
                            break;
                        }
                    }
                }

                if (colorsChanged + modelsChanged == 0) return 0;

                colorsCost = colorsChanged * costPerColorChange;
                modelsCost = modelsChanged * costPerModelChange;

                total = Math.Max(1, (int)Math.Round(colorsCost + modelsCost, 0));

                return total;
            }


            colorsCost = costPerColorChange * colorsChanged;
            modelsCost = costPerModelChange * modelsChanged;
            float cost = colorsCost + modelsCost;
            if(cost == 0) return 0;

            cost = Math.Max(minMaxCost.Item1, Math.Min(minMaxCost.Item2, cost));
            total = Math.Max(1,(int)Math.Round(cost, 0));

            return total;
        }
    }

}