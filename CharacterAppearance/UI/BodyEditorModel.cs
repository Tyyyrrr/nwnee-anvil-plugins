using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using ExtensionsPlugin;

namespace CharacterAppearance.UI
{
    internal sealed class BodyEditorModel : AppearanceEditorModel
    {
        private static readonly Dictionary<CreaturePart, string> _partNames = new()
        {
            {CreaturePart.Head, "Głowa"},
            {CreaturePart.Torso, "Tułów"},
            {CreaturePart.RightBicep, "Ramiona"},
            {CreaturePart.RightForearm, "Przedramiona"}
        };

        private static readonly Dictionary<CreaturePart, string> _tattooNames = new()
        {
            {CreaturePart.Torso, "Tatuaż: Tułów"},
            {CreaturePart.RightBicep, "Tatuaż: Ramiona"},
            {CreaturePart.RightForearm, "Tatuaż: Przedramiona"},
            {CreaturePart.RightThigh, "Tatuaż: Uda"},
            {CreaturePart.RightShin, "Tatuaż: Łydki"}
        };
        

        private readonly NwCreature _pc;
        private readonly EditorFlags _flags;

        private readonly Dictionary<CreaturePart, int> _backupBodyParts, _currentBodyParts;
        private readonly Dictionary<CreaturePart, bool> _backupTattooOverrides, _tattooOverrides;


        private Phenotype _backupPhenotype; 
        private Phenotype _currentPhenotype;

        public Phenotype Phenotype { get => _currentPhenotype; set
            {
                _currentPhenotype = value;
                _pc.Phenotype = value;
            }
        }

        private float _backupBodyHeight;
        private float _currentBodyHeight;
        public float BodyHeight
        {
            get => _currentBodyHeight; set
            {
                _currentBodyHeight = value;
                _pc.VisualTransform.Scale = _currentBodyHeight;
            }
        }

        public float MinimumBodyHeight => ServerData.DataProviders.BodyAppearanceProvider.GetMinMaxBodyHeightForCreature(_pc).Item1;
        public float MaximumBodyHeight => ServerData.DataProviders.BodyAppearanceProvider.GetMinMaxBodyHeightForCreature(_pc).Item2;

        private readonly int[] _backupColors = new int[4];
        private readonly int[] _currentColors = new int[4];

        private readonly int _selectableParts = 0;
        public override int MainEntriesCount => _selectableParts;
        public override IEnumerable<string> MainEntries => _availableParts.Where(kvp=>kvp.Value.Count > 1).Select(kvp => _partNames[kvp.Key])
            .Concat(_availableTattoos.Where(p => _tattooNames.ContainsKey(p)).Select(p => _tattooNames[p]));

        private readonly int _tattooStartIndex = 0;
        private static readonly IList<string> _tattooOptions = new string[] { "  Brak", " Tatuaż" };
        private int _selectableModels = 0;
        public override int SubEntriesCount => _selectableModels;
        public override IEnumerable<string> SubEntries
        {
            get
            {
                if (_selectableParts <= 0) return Array.Empty<string>();

                if (_mainSelection >= _tattooStartIndex) return _tattooOptions;

                return _availableParts[_availableParts.Keys.ElementAt(_mainSelection)].Select(v => v.ToString());
            }
        }

        private bool _leftSide = false;
        public override bool LeftSide
        {
            get => _leftSide;
            set
            {
                if ((!_leftSide && value) || (_leftSide && !value))
                {
                    _leftSide = value;
                    SubSelection = GetCurrentSubSelection();
                }
            }
        }

        public override void CopyToTheOtherSide()
        {
            if (IsSymmetrical) return;

            var currentPart = CurrentPart;
            var oppositePart = GetOppositePart(currentPart);

            if (_mainSelection >= _tattooStartIndex)
            {
                var ovr = _tattooOverrides[currentPart];
                _tattooOverrides[oppositePart] = ovr;
                if (ovr) _pc.SetCreatureBodyPart(oppositePart, _currentBodyParts[oppositePart] + 1);
                else _pc.SetCreatureBodyPart(oppositePart, _currentBodyParts[oppositePart]);
            }
            else
            {
                var modelID = _currentBodyParts[currentPart];
                _currentBodyParts[oppositePart] = modelID;
                if(_tattooOverrides.TryGetValue(oppositePart, out var tatOvr) && tatOvr)
                {
                  _pc.SetCreatureBodyPart(oppositePart, modelID + 1);  
                }
                else _pc.SetCreatureBodyPart(oppositePart, modelID);
            }
        }

        private static readonly CreaturePart[] _doubleParts = new CreaturePart[]{
            CreaturePart.RightShoulder, CreaturePart.LeftShoulder,
            CreaturePart.RightBicep, CreaturePart.LeftBicep,
            CreaturePart.RightForearm, CreaturePart.LeftForearm,
            CreaturePart.RightHand, CreaturePart.LeftHand,
            CreaturePart.RightThigh, CreaturePart.LeftThigh,
            CreaturePart.RightShin, CreaturePart.LeftShin,
            CreaturePart.RightFoot, CreaturePart.LeftFoot
            };

        
        private CreaturePart CurrentPart
        {
            get
            {
                CreaturePart pt;

                if (_mainSelection >= _tattooStartIndex)
                {
                    pt = _availableTattoos[_mainSelection - _tattooStartIndex];
                }

                else pt = _availableParts.ElementAt(_mainSelection).Key;
                
                return LeftSide ? GetOppositePart(pt) : pt;
            }
        }
        

        public override bool IsDoublePart => _doubleParts.Contains(CurrentPart);

        public override bool IsSymmetrical
        {
            get
            {
                if (!IsDoublePart) return true;

                var currentPart = CurrentPart;

                if (_mainSelection >= _tattooStartIndex)
                    return _tattooOverrides[currentPart] == _tattooOverrides[GetOppositePart(currentPart)];
                
                return _currentBodyParts[currentPart] == _currentBodyParts[GetOppositePart(currentPart)];
            }
        }

        private readonly Dictionary<CreaturePart, IList<int>> _availableParts;
        private readonly List<CreaturePart> _availableTattoos;
        public readonly IList<int> AvailableSkinColors;

        public BodyEditorModel(NwCreature pc, EditorFlags flags)
        {
            _pc = pc;
            _flags = flags;

            // store backup
            _backupPhenotype = pc.Phenotype;
            _backupBodyHeight = pc.VisualTransform.Scale;

            _backupColors[0] = pc.GetColor(ColorChannel.Skin);
            _currentColors[0] = _backupColors[0];

            _backupColors[1] = pc.GetColor(ColorChannel.Hair);
            _currentColors[1] = _backupColors[1];
            
            _backupColors[2] = pc.GetColor(ColorChannel.Tattoo1);
            _currentColors[2] = _backupColors[2];
            
            _backupColors[3] = pc.GetColor(ColorChannel.Tattoo2);
            _currentColors[3] = _backupColors[3];


            _backupBodyParts = new();
            foreach (var cp in Enum.GetValues<CreaturePart>())
            {
                _backupBodyParts.Add(cp, _pc.GetCreatureBodyPart(cp));
            }

            _currentBodyParts = new()
            {
                {CreaturePart.Head, _backupBodyParts[CreaturePart.Head]},
                {CreaturePart.Torso, _backupBodyParts[CreaturePart.Torso] == 2 || _backupBodyParts[CreaturePart.Torso] == 202 ? _backupBodyParts[CreaturePart.Torso] - 1 : _backupBodyParts[CreaturePart.Torso]},
                {CreaturePart.RightBicep, _backupBodyParts[CreaturePart.RightBicep] == 2 || _backupBodyParts[CreaturePart.RightBicep] == 215 ? _backupBodyParts[CreaturePart.RightBicep] - 1 : _backupBodyParts[CreaturePart.RightBicep]},
                {CreaturePart.LeftBicep, _backupBodyParts[CreaturePart.LeftBicep] == 2 || _backupBodyParts[CreaturePart.LeftBicep] == 215 ? _backupBodyParts[CreaturePart.LeftBicep] - 1 : _backupBodyParts[CreaturePart.LeftBicep]},
                {CreaturePart.RightForearm, _backupBodyParts[CreaturePart.RightForearm] == 2 ? 1 : _backupBodyParts[CreaturePart.RightForearm]},
                {CreaturePart.LeftForearm, _backupBodyParts[CreaturePart.LeftForearm] == 2 ? 1 : _backupBodyParts[CreaturePart.LeftForearm]},
                {CreaturePart.RightThigh, _backupBodyParts[CreaturePart.RightThigh] == 2 ? 1 : _backupBodyParts[CreaturePart.RightThigh]},
                {CreaturePart.LeftThigh, _backupBodyParts[CreaturePart.LeftThigh] == 2 ? 1 : _backupBodyParts[CreaturePart.LeftThigh]},
                {CreaturePart.RightShin, _backupBodyParts[CreaturePart.RightShin] == 2 ? 1 : _backupBodyParts[CreaturePart.RightShin]},
                {CreaturePart.LeftShin, _backupBodyParts[CreaturePart.LeftShin] == 2 ? 1 : _backupBodyParts[CreaturePart.LeftShin]}
                // other parts are not available in the editor
            };

            _tattooOverrides = new()
            {
                {CreaturePart.Torso, _backupBodyParts[CreaturePart.Torso] == 2 || _backupBodyParts[CreaturePart.Torso] == 202},
                {CreaturePart.RightBicep, _backupBodyParts[CreaturePart.RightBicep] == 2 || _backupBodyParts[CreaturePart.RightBicep] == 215},
                {CreaturePart.LeftBicep, _backupBodyParts[CreaturePart.LeftBicep] == 2 || _backupBodyParts[CreaturePart.LeftBicep] == 215},
                {CreaturePart.RightForearm, _backupBodyParts[CreaturePart.RightForearm] == 2},
                {CreaturePart.LeftForearm, _backupBodyParts[CreaturePart.LeftForearm] == 2},
                {CreaturePart.RightThigh, _backupBodyParts[CreaturePart.RightThigh] == 2},
                {CreaturePart.LeftThigh, _backupBodyParts[CreaturePart.LeftThigh] == 2},
                {CreaturePart.RightShin, _backupBodyParts[CreaturePart.RightShin] == 2},
                {CreaturePart.LeftShin, _backupBodyParts[CreaturePart.LeftShin] == 2}
            };

            foreach(var kvp in _currentBodyParts)
            {
                if(_backupBodyParts.TryGetValue(kvp.Key, out int value) && kvp.Value != value)
                    _backupBodyParts[kvp.Key] = kvp.Value;
            }

            _backupTattooOverrides = new()
            {
                {CreaturePart.Torso,_tattooOverrides[CreaturePart.Torso]},
                {CreaturePart.RightBicep, _tattooOverrides[CreaturePart.RightBicep]},                
                {CreaturePart.LeftBicep, _tattooOverrides[CreaturePart.LeftBicep]},
                {CreaturePart.RightForearm, _tattooOverrides[CreaturePart.RightForearm]},
                {CreaturePart.LeftForearm, _tattooOverrides[CreaturePart.LeftForearm]},
                {CreaturePart.RightThigh, _tattooOverrides[CreaturePart.RightThigh]},
                {CreaturePart.LeftThigh, _tattooOverrides[CreaturePart.LeftThigh]},
                {CreaturePart.RightShin, _tattooOverrides[CreaturePart.RightShin]},
                {CreaturePart.LeftShin, _tattooOverrides[CreaturePart.LeftShin]}
            };

            _availableParts = new();
            _availableTattoos = new List<CreaturePart>();

            int tattooStartIdx = 0;

            if ((_flags & (EditorFlags.Head | EditorFlags.BodyTailor)) != 0)
            {
                _availableParts.Add(CreaturePart.Head, AvailableHeads.GetHeadsForCreature(_pc));
                tattooStartIdx++;
            }

            if (_flags.HasFlag(EditorFlags.BodyTailor))
            {
                var miscParts = ServerData.DataProviders.BodyAppearanceProvider.GetMiscellaneousBodyPartsForCreature(_pc);

                if (miscParts != null)
                {
                    if (miscParts.TryGetValue(CreaturePart.Torso, out var torsoMiscIDs) && torsoMiscIDs.Count > 1)
                    {
                        _availableParts.Add(CreaturePart.Torso, torsoMiscIDs);
                        tattooStartIdx++;
                    }

                    if (miscParts.TryGetValue(CreaturePart.RightBicep, out var bicepMiscIDs) && bicepMiscIDs.Count > 1)
                    {
                        _availableParts.Add(CreaturePart.RightBicep, bicepMiscIDs);
                        tattooStartIdx++;
                    }

                    if (miscParts.TryGetValue(CreaturePart.RightForearm, out var forearmMiscIDs) && forearmMiscIDs.Count > 1)
                    {
                        _availableParts.Add(CreaturePart.RightForearm, forearmMiscIDs);
                        tattooStartIdx++;
                    }
                }
            }

            _tattooStartIndex = tattooStartIdx;

            if (((_flags & (EditorFlags.Tattoo | EditorFlags.BodyTailor)) != 0) && _pc.Appearance.RowIndex != 1002) // kobolds can't have tatoos
            {
                _availableTattoos.AddRange(new CreaturePart[] { CreaturePart.Torso, CreaturePart.RightBicep, CreaturePart.RightForearm});

                if (!_pc.IsSatyr()) _availableTattoos.AddRange(new CreaturePart[] { CreaturePart.RightThigh, CreaturePart.RightShin});
            }

            if ((_flags & (EditorFlags.SkinColor | EditorFlags.BodyTailor)) != 0) 
                AvailableSkinColors = ServerData.DataProviders.BodyAppearanceProvider.GetSkinColorsForCreature(_pc).ToList();
            else AvailableSkinColors = ServerData.IBodyAppearanceProvider.AllColors.ToList();

            _currentBodyHeight = _pc.VisualTransform.Scale;
            _currentPhenotype = _pc.Phenotype;

            if(_availableParts.Count + _availableTattoos.Count > 0)
            {
                MainSelection = 0;
                _selectableParts = MainEntries.Count();
                _selectableModels = SubEntries.Count();
            }
        }

        public void RestoreBackup()
        {
            _pc.Phenotype = _backupPhenotype;
            _currentPhenotype = _backupPhenotype;
            _pc.VisualTransform.Scale = _backupBodyHeight;
            _currentBodyHeight = _backupBodyHeight;

            _pc.SetColor(ColorChannel.Skin, _backupColors[0]);
            _pc.SetColor(ColorChannel.Hair, _backupColors[1]);
            _pc.SetColor(ColorChannel.Tattoo1, _backupColors[2]);
            _pc.SetColor(ColorChannel.Tattoo2, _backupColors[3]);

            _backupColors.CopyTo(_currentColors,0);

            foreach(var kvp in _backupBodyParts)
            {
                bool hasTattoo = _backupTattooOverrides.TryGetValue(kvp.Key, out var tatOvr) && tatOvr == true;

                _pc.SetCreatureBodyPart(kvp.Key, kvp.Value + (hasTattoo ? 1 : 0));

                if(_currentBodyParts.ContainsKey(kvp.Key)) 
                    _currentBodyParts[kvp.Key] = kvp.Value;
            }

            foreach(var kvp in _backupTattooOverrides) 
                _tattooOverrides[kvp.Key] = kvp.Value;
        }

        private int GetCurrentSubSelection()
        {
            if (_mainSelection >= _tattooStartIndex)
            {
                return _tattooOverrides[CurrentPart] ? 1 : 0;
            }
            else
            {
                try{
                    var ids = _availableParts.Values.ElementAt(_mainSelection);

                    return ids.IndexOf(_currentBodyParts[CurrentPart]);
                }
                catch (ArgumentOutOfRangeException)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error("Current sub selection out of range. Fallback to default index 0.");
                    return 0;
                }
            }
        }
        
        void UpdateBodyPart()
        {
            CreaturePart currentPart = CurrentPart;

            if (_tattooOverrides.TryGetValue(CurrentPart, out var ovr) && ovr)
                _pc.SetCreatureBodyPart(currentPart, _currentBodyParts[currentPart] + 1);

            else _pc.SetCreatureBodyPart(currentPart, _currentBodyParts[currentPart]);
        }


        private int _mainSelection = -1;
        public override int MainSelection
        {
            get => _mainSelection;
            set
            {
                _mainSelection = value;

                if (!IsDoublePart)
                    LeftSide = false;
                    
                SubSelection = GetCurrentSubSelection();
                _selectableModels = SubEntries.Count();
            }
        }

        private int _subSelection = -1;
        public override int SubSelection
        {
            get => _subSelection;
            set
            {
                if (_mainSelection >= _tattooStartIndex)
                {
                    _tattooOverrides[CurrentPart] = value > 0;
                }
                else
                {
                    _currentBodyParts[CurrentPart] = _availableParts[LeftSide ? GetOppositePart(CurrentPart) : CurrentPart].ElementAtOrDefault(value);
                }

                _subSelection = value;
                UpdateBodyPart();
                
            }
        }

        private int _selectedColorChannel = -1;
        public override int SelectedColorChannel
        {
            get => _selectedColorChannel;
            set
            {
                if(value != _selectedColorChannel && (value == 0 || value == 2 || value == 4 || value == 5))
                {
                    _selectedColorChannel = value;
                }
                else _selectedColorChannel = -1;
            }
        }


        public override int SelectedColorIndex {
            get => _selectedColorChannel switch
                {
                    0 => _currentColors[0],
                    2 => _currentColors[1],
                    4 => _currentColors[2],
                    5 => _currentColors[3],
                    _=>255  
                };

            set
            {
                if(_selectedColorChannel < 0 || _selectedColorChannel > 5 || value < 0 || value > 175) 
                    return;
                
                var arrId = _selectedColorChannel switch
                {
                    0 => 0,
                    2=>1,
                    4=>2,
                    5=>3,
                    _=>-1  
                };
                if(arrId < 0) return;
                _currentColors[arrId] = value;
                _pc.SetColor((ColorChannel)arrId, value);
            }
        }

        public override ColorChart CurrentColorChart => _selectedColorChannel switch
        {
            0 => ColorChart.Skin,
            2 => ColorChart.Hair,
            _ => ColorChart.Tattoo
        };

        public override bool IsValidColor(int colorID)
        {
            if(CurrentColorChart == ColorChart.Skin) return AvailableSkinColors.Contains(colorID);

            return colorID >= 0 && colorID < 11*16;
        }


        public bool CanEditSkinColor => (_flags & (EditorFlags.BodyTailor | EditorFlags.SkinColor)) != 0;
        public bool CanEditHairColor => (_flags & (EditorFlags.BodyTailor | EditorFlags.HairColor | EditorFlags.Head)) != 0;
        public bool CanEditTattooColor => (_flags & (EditorFlags.BodyTailor | EditorFlags.Tattoo)) != 0;

        public bool CanEditPhenotype => (_flags & (EditorFlags.BodyTailor | EditorFlags.Phenotype)) != 0;
        public bool CanEditBodyHeight => _flags.HasFlag(EditorFlags.BodyTailor);

        public override int AppearanceChangeCost
        {
            get
            {
                int hairChangeCost = CharacterAppearanceService.HairChangeCost;
                int hairColorChangeCost = CharacterAppearanceService.HairColorChangeCost;
                int tattooAddCost = CharacterAppearanceService.TattooCreateCost;
                int tattooRemoveCost = CharacterAppearanceService.TattooRemoveCost;
                int tattooColorChangeCost = CharacterAppearanceService.TattooColorChangeCost;

                if(new int[]{hairChangeCost, hairColorChangeCost, tattooAddCost, tattooRemoveCost, tattooColorChangeCost}.Any(i=>i<0))
                    return -1;

                bool tattooColorChanged = false;
                bool hairColorChanged = false;
                bool headModelChanged = false;
                int tattoosAdded = 0;
                int tattoosRemoved = 0;

                foreach(var kvp in _backupTattooOverrides)
                {
                    if(_tattooOverrides[kvp.Key] != kvp.Value)
                    {
                        if(kvp.Value == false) tattoosAdded++;
                        else tattoosRemoved++;
                    }
                }
                
                if(_backupBodyParts.TryGetValue(CreaturePart.Head, out var originalHead) && _currentBodyParts.TryGetValue(CreaturePart.Head, out var newHead) && originalHead != newHead)
                    headModelChanged = true;

                if(_backupColors[(int)ColorChannel.Hair] != _currentColors[(int)ColorChannel.Hair])
                    hairColorChanged = true;

                if(_backupColors[(int)ColorChannel.Tattoo1] != _currentColors[(int)ColorChannel.Tattoo1] || _backupColors[(int)ColorChannel.Tattoo2] != _currentColors[(int)ColorChannel.Tattoo2])
                    tattooColorChanged = true;

                int total = 0;

                if (tattooColorChanged)
                {
                    int originalTattoosCount = _backupTattooOverrides.Count(kvp=>kvp.Value == true);

                    if(originalTattoosCount == 0) total += tattooColorChangeCost;
                    else total = originalTattoosCount * tattooRemoveCost + tattooColorChangeCost;
                }

                total += tattoosRemoved * tattooRemoveCost + tattoosAdded * tattooAddCost;

                if(hairColorChanged) total += hairColorChangeCost;

                if(headModelChanged) total += hairChangeCost;

                return total;
            }
        }

        public override bool IsDirty => 
        _backupPhenotype != _currentPhenotype
        || _backupBodyHeight != _currentBodyHeight
        || !_backupColors.SequenceEqual(_currentColors)
        || _currentBodyParts.Any(kvp=>_backupBodyParts[kvp.Key] != kvp.Value)
        || _backupTattooOverrides.Any(kvp => _tattooOverrides[kvp.Key] != kvp.Value);

        public override void ApplyChanges()
        {
            _backupPhenotype = _currentPhenotype;
            _backupBodyHeight = _currentBodyHeight;

            _currentColors.CopyTo(_backupColors,0);

            foreach(var kvp in _currentBodyParts) _backupBodyParts[kvp.Key] = kvp.Value;

            foreach(var kvp in _tattooOverrides) _backupTattooOverrides[kvp.Key] = kvp.Value;

            var player = _pc.ControllingPlayer;

            if(player != null && player.IsValid)
                CharacterAppearanceService.RaiseOnBodyAppearanceEditComplete(player, true);
        }

        public override void RevertChanges(){
            RestoreBackup();
            SubSelection = GetCurrentSubSelection();
        }
    }
}