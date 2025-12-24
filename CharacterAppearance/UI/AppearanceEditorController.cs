using System;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using ExtensionsPlugin;
using NuiMVC;

using View = CharacterAppearance.UI.AppearanceEditorView;

namespace CharacterAppearance.UI
{
    internal sealed class AppearanceEditorController : ControllerBase, IDisposable
    {
        private readonly NwCreature _pc;
        private readonly NwArea? _initialArea = null;
        private readonly NwPlayer _player;
        internal NwPlayer GetPlayer() => _player;

        private readonly EditorFlags _flags;   
        
        void OnClientLeave(ModuleEvents.OnClientLeave data)
        {
            _shouldRestoreBackup = true;
            Close();
            
        }
        void OnClientDisconnect(OnClientDisconnect data)
        {
            _shouldRestoreBackup = true;
            Close();
        }

        public AppearanceEditorController(NwPlayer player, NuiWindow window, EditorFlags flags) : base(player, window)
        {
            _player = player;
            if(!_player.IsValid) throw new InvalidOperationException("The player is not valid");
            _flags = flags;
            if (flags == 0) throw new ArgumentException("Invalid flags for AppearanceEditor NUI.", nameof(flags));

            _player.OnClientLeave += OnClientLeave;
            _player.OnClientDisconnect += OnClientDisconnect;

            _pc = player.ControlledCreature ?? throw new InvalidOperationException("No creature under control of player");
            if(!_pc.IsValid) throw new InvalidOperationException("Creature is not valid");


            if ((flags & (EditorFlags.Head | EditorFlags.Tattoo | EditorFlags.HairColor | EditorFlags.SkinColor | EditorFlags.HairColor | EditorFlags.Phenotype | EditorFlags.BodyTailor)) != 0)
            {
                _bodyModel = new BodyEditorModel(_pc, flags);
            }
            else  _bodyModel = null;
            
            if ((flags & EditorFlags.Armor) != 0)
            {
                _armorModel = new ArmorEditorModel(_pc, _flags);
                _armorModel.OnDirty += Refresh;
            }
            else
            {
                _armorModel?.Dispose();
                _armorModel = null;
            }

            if ((flags & EditorFlags.Weapon) != 0)
            {
                _weaponModel = new WeaponEditorModel(_pc, _flags);
                _weaponModel.OnDirty += Refresh;
            }
            else
            {
                _weaponModel?.Dispose();
                _weaponModel = null;
            }

            if (_bodyModel != null)
            {
                EditBody();
            }
            else if (_armorModel != null)
            {
                EditArmor();
            }
            else if (_weaponModel != null)
            {
                EditWeapon();
            }
            else currentModel = null;

            SetValue((NuiBind<NuiRect>)View.Window.Geometry, new NuiRect(1, 1, 330, 550));

            _initialArea = _pc.Area;
        }

        static string GetColorChartResRef(AppearanceEditorModel.ColorChart chart) => chart switch
        {
            AppearanceEditorModel.ColorChart.Skin => View.COLOR_PALETTE_SKIN,
            AppearanceEditorModel.ColorChart.Hair => View.COLOR_PALETTE_HAIR,
            AppearanceEditorModel.ColorChart.Tattoo => View.COLOR_PALETTE_TATTOO,
            AppearanceEditorModel.ColorChart.Cloth => View.COLOR_PALETTE_CLOTH,
            AppearanceEditorModel.ColorChart.Leather => View.COLOR_PALETTE_LEATHER,
            AppearanceEditorModel.ColorChart.Metal => View.COLOR_PALETTE_METAL,
            _ => string.Empty
        };
        
        private void Refresh()
        {
            if(currentModel == null) return;

            SetWatch(View.SlotComboSelectedProperty, false);
            SetWatch(View.ValueComboSelectedProperty, false);

            SetValue(View.SlotComboEntriesProperty, currentModel.MainEntries.Select((e,i)=>new NuiComboEntry(e,i)).ToList());
            SetValue(View.ValueComboEntriesProperty, currentModel.SubEntries.Select((e,i)=>new NuiComboEntry(e,i)).ToList());

            SetValue(View.SlotComboSelectedProperty, currentModel.MainSelection);
            SetValue(View.ValueComboSelectedProperty, currentModel.SubSelection);

            SetValue(View.SlotComboEnabledProperty, currentModel.MainEntriesCount > 1);
            SetValue(View.ValueComboEnabledProperty, currentModel.SubEntriesCount > 1);

            // Refresh arrow buttons
            SetValue(View.ArrowButtonLeftEnabledProperty, currentModel.SubEntriesCount > 1 && currentModel.SubSelection > 0);
            SetValue(View.ArrowButtonRightEnabledProperty, currentModel.SubEntriesCount > 1 && currentModel.SubSelection < currentModel.SubEntriesCount - 1);

            // Refresh Apply and Restore buttons
            if (_flags.HasFlag(EditorFlags.FreeOfCharge))
            {
                SetValue(View.RestoreButtonEnabledProperty, currentModel.IsDirty);
                SetValue(View.ApplyButtonEnabledProperty, currentModel.IsDirty);
                SetValue(View.ApplyButtonLabelProperty,"Zastosuj");
            }
            else
            {
                var pcGold = _pc.Gold;
                var goldToPay = currentModel.AppearanceChangeCost;

                SetValue(View.RestoreButtonEnabledProperty, currentModel.IsDirty);
                SetValue(View.ApplyButtonEnabledProperty, _pc.Gold >= goldToPay && currentModel.IsDirty);
                SetValue(View.ApplyButtonLabelProperty, goldToPay > 0 ? $"Koszt: {goldToPay}" : "Zastosuj");
            }

            
            // Refresh symmetry buttons
            if (currentModel.MainEntriesCount > 0 && currentModel.IsDoublePart)
            {
                if(currentModel == _weaponModel) 
                    SetValue(View.SymmetryButtonVisibleProperty, false);
                else SetValue(View.SymmetryButtonVisibleProperty, true);

                SetValue(View.LRSideButtonsVisibleProperty, true);
                SetValue(View.SymmetryButtonEnabledProperty, !currentModel.IsSymmetrical);
            }
            else
            {
                SetValue(View.SymmetryButtonVisibleProperty, false);
                SetValue(View.LRSideButtonsVisibleProperty, false);
            }

            SetValue(View.LeftSideButtonEnabledProperty, !currentModel.LeftSide);
            SetValue(View.LeftSideButtonSelectedProperty, currentModel.LeftSide);
            SetValue(View.RightSideButtonEnabledProperty, currentModel.LeftSide);
            SetValue(View.RightSideButtonSelectedProperty, !currentModel.LeftSide);

            SetValue(View.SymmetryButtonTooltipProperty, $"Skopiuj na {(currentModel.LeftSide ? "prawą" : "lewą")} stronę.");

            (int,int) coords;
            // Refresh ColorImages
            for(int i = 0; i < 6; i++)
            {
                var selChan = currentModel.SelectedColorChannel;

                SetValue(View.ColorImageEncouragedProperties[i], selChan == i);

                currentModel.SelectedColorChannel = i;

                SetValue(View.ColorImageResRefProperties[i], GetColorChartResRef(currentModel.CurrentColorChart));

                if(currentModel.SelectedColorChannel >= 0 && currentModel.SelectedColorIndex >= 0 && currentModel.SelectedColorIndex < 11*16)
                {
                    coords = currentModel.CurrentColorCoords;
                    
                    if(currentModel == _bodyModel)
                        SetValue(View.ColorImageRectProperties[i], new NuiRect(coords.Item1 * View.SingleColorSquareSize, coords.Item2 * View.SingleColorSquareSize, View.SingleColorSquareSize, View.SingleColorSquareSize));
                    else SetValue(View.ColorImageRectProperties[i], new NuiRect(coords.Item1 * View.SingleColorSquareSize, coords.Item2 * View.SingleColorSquareSize, View.SingleColorSquareSize + 7, View.SingleColorSquareSize)); // some difference in color chart image (?)
                    
                }
                else if(currentModel == _bodyModel)
                    SetValue(View.ColorImageRectProperties[i], new NuiRect(10*View.SingleColorSquareSize+View.SingleColorSquareSize/2,15*View.SingleColorSquareSize+View.SingleColorSquareSize/2,View.SingleColorSquareSize, View.SingleColorSquareSize));
                else
                    SetValue(View.ColorImageRectProperties[i], new NuiRect(10*View.SingleColorSquareSize+View.SingleColorSquareSize/2,15*View.SingleColorSquareSize+View.SingleColorSquareSize/2,View.SingleColorSquareSize + 7, View.SingleColorSquareSize));
                 
                currentModel.SelectedColorChannel = selChan;
            }

            // Refresh color palette
            if(currentModel.SelectedColorChannel < 0 || currentModel.SelectedColorChannel > 5)
            {
                SetValue(View.ColorPaletteVisibleProperty, false);
                
                SetWatch(View.SlotComboSelectedProperty, true);
                SetWatch(View.ValueComboSelectedProperty, true);
                return;
            }

            SetValue(View.ColorPaletteResRefProperty, GetColorChartResRef(currentModel.CurrentColorChart));
            
            coords = currentModel.CurrentColorCoords;

            for(int i = 0; i < 11*16; i++)
            {
                if(coords == i.Inflate()) SetValue((NuiBind<bool>)View.ColorPaletteImages[i].Encouraged!, true);
                else SetValue((NuiBind<bool>)View.ColorPaletteImages[i].Encouraged!, false);
                SetValue((NuiBind<bool>)View.ColorPaletteImages[i].Enabled!, currentModel.IsValidColor(i));

                var c = i.Inflate();
                if(currentModel == _bodyModel)
                    SetValue((NuiBind<NuiRect>)View.ColorPaletteImages[i].ImageRegion!, new NuiRect(c.Item1*View.SingleColorSquareSize, c.Item2*View.SingleColorSquareSize, View.SingleColorSquareSize, View.SingleColorSquareSize));
                else
                    SetValue((NuiBind<NuiRect>)View.ColorPaletteImages[i].ImageRegion!, new NuiRect(c.Item1*View.SingleColorSquareSize, c.Item2*View.SingleColorSquareSize, View.SingleColorSquareSize + 7, View.SingleColorSquareSize));
            }

            SetValue(View.ColorPaletteVisibleProperty, true);

            SetWatch(View.SlotComboSelectedProperty, true);
            SetWatch(View.ValueComboSelectedProperty, true);
        }

        private void EditBody()
        {
            ClearArmorLayout();
            ClearWeaponLayout();

            currentModel = _bodyModel;

            if(_bodyModel == null) return;

            SetValue(View.GeneralButtonBodySelectedProperty, true);
            SetValue(View.GeneralButtonBodyEnabledProperty, false);

            SetValue(View.GeneralButtonArmorSelectedProperty, false);
            SetValue(View.GeneralButtonArmorEnabledProperty, _armorModel != null);

            SetValue(View.GeneralButtonWeaponSelectedProperty, false);
            SetValue(View.GeneralButtonWeaponEnabledProperty, _weaponModel != null);

            SetValue(View.ColorImageVisibleProperties[0], true);
            if (_bodyModel.CanEditSkinColor)
            {
                SetValue(View.ColorImageEnabledProperties[0], true);
                SetValue(View.ColorImageTooltipProperties[0], "Kolor skóry");
            }
            else SetValue(View.ColorImageEnabledProperties[0], false);
            

                SetValue(View.ColorImageVisibleProperties[2], true);
            if (_bodyModel.CanEditHairColor)
            {
                SetValue(View.ColorImageEnabledProperties[2], true);
                SetValue(View.ColorImageTooltipProperties[2], "Kolor włosów");
            }
            else SetValue(View.ColorImageEnabledProperties[2], false);
            

            SetValue(View.ColorImageVisibleProperties[4], true);
            SetValue(View.ColorImageVisibleProperties[5], true);
            if (_bodyModel.CanEditTattooColor)
            {
                SetValue(View.ColorImageEnabledProperties[4], true);
                SetValue(View.ColorImageEnabledProperties[5], true);

                SetValue(View.ColorImageTooltipProperties[4], "Kolor tatuażu I");
                SetValue(View.ColorImageTooltipProperties[5], "Kolor tatuażu II");
            }
            else
            {
                SetValue(View.ColorImageEnabledProperties[4], false);
                SetValue(View.ColorImageEnabledProperties[5], false);
            }

            SetValue(View.ColorImageVisibleProperties[1], false);
            SetValue(View.ColorImageVisibleProperties[3], false);
            SetValue(View.ColorImageEnabledProperties[1], false);
            SetValue(View.ColorImageEnabledProperties[3], false);

            SetValue(View.RightSideButtonLabelProperty, "Prawa strona");
            SetValue(View.LeftSideButtonLabelProperty, "Lewa strona");

            if (_bodyModel.CanEditBodyHeight)
            {
                SetValue(View.BodyHeightSliderMinProperty, _bodyModel.MinimumBodyHeight);
                SetValue(View.BodyHeightSliderMaxProperty, _bodyModel.MaximumBodyHeight);

                SetValue(View.BodyHeightSliderValueProperty, _bodyModel.BodyHeight);
                SetValue(View.BodyHeightSliderVisibleProperty, true);

                SetWatch(View.BodyHeightSliderValueProperty, true);
            }
            else
            {
                SetWatch(View.BodyHeightSliderValueProperty, false);
                SetValue(View.BodyHeightSliderVisibleProperty, false);
            }

            if (_bodyModel.CanEditPhenotype)
            {
                SetValue(View.PhenotypesVisibleProperty, true);

                SetValue(View.Phenotype1SelectedProperty, _bodyModel.Phenotype == Phenotype.Normal || (int)_bodyModel.Phenotype == 16);
                SetValue(View.Phenotype1EnabledProperty, _bodyModel.Phenotype != Phenotype.Normal && (int)_bodyModel.Phenotype != 16);
                SetValue(View.Phenotype2SelectedProperty, _bodyModel.Phenotype == Phenotype.Big || (int)_bodyModel.Phenotype == 25);
                SetValue(View.Phenotype2EnabledProperty, _bodyModel.Phenotype != Phenotype.Big && (int)_bodyModel.Phenotype != 25);

                SetWatch(View.Phenotype1SelectedProperty, true);
                SetWatch(View.Phenotype2SelectedProperty, true);
            }
            else
            {
                SetWatch(View.Phenotype1SelectedProperty, false);
                SetWatch(View.Phenotype2SelectedProperty, false);

                SetValue(View.PhenotypesVisibleProperty, false);
            }

            Refresh();
        }
        
        private void ClearBodyLayout()
        {
            if (_bodyModel == null) return;

            SetWatch(View.Phenotype1SelectedProperty, false);
            SetWatch(View.Phenotype2SelectedProperty, false);
            SetValue(View.PhenotypesVisibleProperty, false);

            SetWatch(View.BodyHeightSliderValueProperty, false);
            SetValue(View.BodyHeightSliderVisibleProperty, false);
        }

        private void EditArmor()
        {
            ClearBodyLayout();
            ClearWeaponLayout();

            currentModel = _armorModel;

            if(_armorModel == null) return;

            SetValue(View.GeneralButtonArmorSelectedProperty, true);
            SetValue(View.GeneralButtonArmorEnabledProperty, false);

            SetValue(View.GeneralButtonBodySelectedProperty, false);
            SetValue(View.GeneralButtonBodyEnabledProperty, _bodyModel != null);

            SetValue(View.GeneralButtonWeaponSelectedProperty, false);
            SetValue(View.GeneralButtonWeaponEnabledProperty, _weaponModel != null);

            SetValue(View.ColorImageTooltipProperties[0], "Materiał I");
            SetValue(View.ColorImageTooltipProperties[1], "Materiał II");
            SetValue(View.ColorImageTooltipProperties[2], "Skóra I");
            SetValue(View.ColorImageTooltipProperties[3], "Skóra II");
            SetValue(View.ColorImageTooltipProperties[4], "Metal I");
            SetValue(View.ColorImageTooltipProperties[5], "Metal II");

            SetValue(View.RightSideButtonLabelProperty, "Prawa strona");
            SetValue(View.LeftSideButtonLabelProperty, "Lewa strona");

            for(int i = 0; i < 6; i++){
                SetValue(View.ColorImageEnabledProperties[i], true);
                SetValue(View.ColorImageVisibleProperties[i], true);
            }
            
            Refresh();
        }
        
        private void ClearArmorLayout()
        {
            if (_armorModel == null) return;

            for (int i = 0; i < 6; i++)
            {
                SetValue(View.ColorImageEnabledProperties[i], false);
                SetValue(View.ColorImageVisibleProperties[i], false);
                SetValue(View.ColorImageEncouragedProperties[i], false);
            }

        }

        private void EditWeapon()
        {
            ClearBodyLayout();
            ClearArmorLayout();

            currentModel = _weaponModel;

            if(_weaponModel == null) return;


            SetValue(View.GeneralButtonBodySelectedProperty, false);
            SetValue(View.GeneralButtonBodyEnabledProperty, _bodyModel != null);

            SetValue(View.GeneralButtonArmorSelectedProperty, false);
            SetValue(View.GeneralButtonArmorEnabledProperty, _armorModel != null);

            SetValue(View.GeneralButtonWeaponSelectedProperty, true);
            SetValue(View.GeneralButtonWeaponEnabledProperty, false);

            SetValue(View.RightSideButtonLabelProperty, "Broń");
            SetValue(View.LeftSideButtonLabelProperty, "Tarcza");

            foreach(var e in View.ColorImageVisibleProperties)
                SetValue(e, false);

            Refresh();
        }

        private void ClearWeaponLayout()
        {
            if(_weaponModel == null) return;

            for (int i = 0; i < 6; i++)
            {
                SetValue(View.ColorImageEnabledProperties[i], false);
                SetValue(View.ColorImageVisibleProperties[i], false);
                SetValue(View.ColorImageEncouragedProperties[i], false);
            }
        }


        private BodyEditorModel? _bodyModel;
        private ArmorEditorModel? _armorModel;
        private WeaponEditorModel? _weaponModel;

        private AppearanceEditorModel? currentModel;


        private void CloseIfNotInInitialArea()
        {
            if(_pc.IsValid && _pc.Area != _initialArea)
            {
                Close();
                if(_player.IsValid)
                    _player.SendServerMessage("Zamknięto okno edytora wyglądu z powodu zmiany lokacji.", ColorConstants.Red);
                return;
            }
        }

        protected override void OnClick(string elementId)
        {
            CloseIfNotInInitialArea();

            if (currentModel == null) return;

            switch (elementId)
            {
                case nameof(View.ApplyButton): 
                    {
                        var goldToPay = _flags.HasFlag(EditorFlags.FreeOfCharge) ? 0 : currentModel.AppearanceChangeCost;
                        if(goldToPay > _pc.Gold)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Error("Player have not enough gold to apply appearance changes! (invalid controller state - button should be disabled)");
                            _player.SendServerMessage("Nie masz wystarczającej ilości złota", ColorConstants.Red);
                        }
                        else
                        {
                            currentModel.ApplyChanges();

                            if(goldToPay > 0) _pc.TakeGold(goldToPay);
                        }
                    }
                    Refresh();
                    break;
                    
                case nameof(View.RestoreButton):
                    currentModel.RevertChanges();
                    Refresh();
                    break;

                case nameof(View.CancelButton): _shouldRestoreBackup = true; Close(); break;
                    
                // todo:
                case nameof(View.GeneralButtonBody): EditBody(); break;
                case nameof(View.GeneralButtonArmor): EditArmor(); break;
                case nameof(View.GeneralButtonWeapon): EditWeapon(); break;

                case nameof(View.ArrowButtonLeft): SetValue(View.ValueComboSelectedProperty, currentModel.SubSelection - 1); break;
                case nameof(View.ArrowButtonRight): SetValue(View.ValueComboSelectedProperty, currentModel.SubSelection + 1); break;

                case nameof(View.LeftSideButton):
                    currentModel.LeftSide = true;
                    Refresh();
                    break;
                

                case nameof(View.RightSideButton):
                    currentModel.LeftSide = false;
                    Refresh();
                    break;

                case nameof(View.SymmetryButton):

                    currentModel.CopyToTheOtherSide();
                    Refresh();
                    break;
            }
        }

        protected override void OnMouseDown(string elementId)
        {
            CloseIfNotInInitialArea();

            if (currentModel == null) return;

            if (elementId.StartsWith("ColImg"))
            {
                int chanIndex = int.Parse(elementId["ColImg".Length..]);

                currentModel.SelectedColorChannel = chanIndex;

                Refresh();
            }
            else if(elementId.StartsWith("ColPalImg"))
            {
                var split = elementId.Split('_');
                var coords = (int.Parse(split[2]), int.Parse(split[1]));
                var col = coords.Flatten();

                currentModel.SelectedColorIndex = col;

                Refresh();
            }
        }

        protected override void Update(string elementId)
        {
            CloseIfNotInInitialArea();

            if (currentModel == null) return;

            switch (elementId)
            {
                case nameof(View.SlotComboSelectedProperty):
                    {
                        var value = GetValue(View.SlotComboSelectedProperty);
                        currentModel.MainSelection = value;
                        Refresh();
                    }
                    break;

                case nameof(View.ValueComboSelectedProperty):
                    {
                        var value = GetValue(View.ValueComboSelectedProperty);
                        currentModel.SubSelection = value;
                        Refresh();
                    }
                    break;



                case nameof(View.Phenotype1SelectedProperty):
                    {
                        if(_bodyModel == null) break;

                        var value = GetValue(View.Phenotype1SelectedProperty);

                        if (!value || _bodyModel.Phenotype == Phenotype.Normal || (int)_bodyModel.Phenotype == 16) break;

                        _bodyModel.Phenotype = (int)_bodyModel.Phenotype == 25 ? (Phenotype)16 : Phenotype.Normal;

                        SetValue(View.Phenotype1SelectedProperty, value);
                        SetValue(View.Phenotype1EnabledProperty, !value);
                        SetValue(View.Phenotype2SelectedProperty, !value);
                        SetValue(View.Phenotype2EnabledProperty, value);
                        Refresh();
                    }
                    break;

                case nameof(View.Phenotype2SelectedProperty):
                    {
                        if(_bodyModel == null) break;

                        var value = GetValue(View.Phenotype2SelectedProperty);

                        if (!value || _bodyModel.Phenotype == Phenotype.Big || (int)_bodyModel.Phenotype == 25) break;

                        _bodyModel.Phenotype = (int)_bodyModel.Phenotype == 16 ? (Phenotype)25 : Phenotype.Big;

                        SetValue(View.Phenotype2SelectedProperty, value);
                        SetValue(View.Phenotype2EnabledProperty, !value);
                        SetValue(View.Phenotype1SelectedProperty, !value);
                        SetValue(View.Phenotype1EnabledProperty, value);
                        Refresh();
                    }
                    break;

                case nameof(View.BodyHeightSliderValueProperty):
                    if(_bodyModel == null) break;
                    _bodyModel.BodyHeight = GetValue(View.BodyHeightSliderValueProperty);
                    Refresh();
                    break;
            }
        }

        private bool _shouldRestoreBackup = true;
        protected override object? OnClose()
        {
            if(_shouldRestoreBackup){
                _bodyModel?.RestoreBackup();
                _armorModel?.RevertChanges();
                _weaponModel?.RevertChanges();
                Dispose();
                return false;
            }

            Dispose();
            _shouldRestoreBackup = true;
            return true;
        }

        private bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed) return;

            if(_player.IsValid){            
                _player.OnClientLeave -= OnClientLeave;
                _player.OnClientDisconnect -= OnClientDisconnect;
            }

            isDisposed = true;

            if(_armorModel != null)
            {
                _armorModel.OnDirty -= Refresh;
                _armorModel.Dispose();
            }

            if(_weaponModel != null)
            {
                _weaponModel.OnDirty -= Refresh;
                _weaponModel.Dispose();
            }
        }
    }
}