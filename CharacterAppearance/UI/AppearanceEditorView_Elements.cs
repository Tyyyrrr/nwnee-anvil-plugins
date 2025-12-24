using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace CharacterAppearance.UI
{
    internal static partial class AppearanceEditorView
    {
        #region General Buttons
        public static readonly NuiBind<bool> GeneralButtonBodyEnabledProperty = NuiProperty<bool>.CreateBind(nameof(GeneralButtonBodyEnabledProperty));
        public static readonly NuiBind<bool> GeneralButtonBodySelectedProperty = NuiProperty<bool>.CreateBind(nameof(GeneralButtonBodySelectedProperty));
        public static readonly NuiBind<string> GeneralButtonBodyDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(GeneralButtonBodyDisabledTooltipProperty));
        public static readonly NuiButtonSelect GeneralButtonBody = new("Ciało", GeneralButtonBodySelectedProperty)
        {
            Id = nameof(GeneralButtonBody),
            Enabled = GeneralButtonBodyEnabledProperty,
            Encouraged = GeneralButtonBodySelectedProperty,
            DisabledTooltip = GeneralButtonBodyDisabledTooltipProperty,
            Width = GeneralButtonWidth,
            Height = GeneralButtonHeight,
            Padding = 0,
            Margin = 0
        };

        public static readonly NuiBind<bool> GeneralButtonArmorEnabledProperty = NuiProperty<bool>.CreateBind(nameof(GeneralButtonArmorEnabledProperty));
        public static readonly NuiBind<bool> GeneralButtonArmorSelectedProperty = NuiProperty<bool>.CreateBind(nameof(GeneralButtonArmorSelectedProperty));
        public static readonly NuiBind<string> GeneralButtonArmorDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(GeneralButtonArmorDisabledTooltipProperty));
        public static readonly NuiButtonSelect GeneralButtonArmor = new("Ubiór",GeneralButtonArmorSelectedProperty)
        {
            Id = nameof(GeneralButtonArmor),
            Enabled = GeneralButtonArmorEnabledProperty,
            Encouraged = GeneralButtonArmorSelectedProperty,
            DisabledTooltip = GeneralButtonArmorDisabledTooltipProperty,
            Width = GeneralButtonWidth,
            Height = GeneralButtonHeight,
            Padding = 0,
            Margin = 0
        };

        public static readonly NuiBind<bool> GeneralButtonWeaponEnabledProperty = NuiProperty<bool>.CreateBind(nameof(GeneralButtonWeaponEnabledProperty));
        public static readonly NuiBind<bool> GeneralButtonWeaponSelectedProperty = NuiProperty<bool>.CreateBind(nameof(GeneralButtonWeaponSelectedProperty));
        public static readonly NuiBind<string> GeneralButtonWeaponDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(GeneralButtonWeaponDisabledTooltipProperty));
        public static readonly NuiButtonSelect GeneralButtonWeapon = new("Broń",GeneralButtonWeaponSelectedProperty)
        {
            Id = nameof(GeneralButtonWeapon),
            Enabled = GeneralButtonWeaponEnabledProperty,
            Encouraged = GeneralButtonWeaponSelectedProperty,
            DisabledTooltip = GeneralButtonWeaponDisabledTooltipProperty,
            Width = GeneralButtonWidth,
            Height = GeneralButtonHeight,
            Padding = 0,
            Margin = 0
        };
        #endregion

        #region Apply, Restore, Quit Buttons
        public static readonly NuiBind<string> ApplyButtonLabelProperty = NuiProperty<string>.CreateBind(nameof(ApplyButtonLabelProperty));
        public static readonly NuiBind<bool> ApplyButtonEnabledProperty = NuiProperty<bool>.CreateBind(nameof(ApplyButtonEnabledProperty));
        public static readonly NuiButton ApplyButton = new(ApplyButtonLabelProperty)
        { 
            Id = nameof(ApplyButton),
            Width = ApplyCancelButtonWidth,
            Height = ApplyCancelButtonHeight,
            Enabled = ApplyButtonEnabledProperty,
            Padding = 0,
            Margin = 0
        };

        public static readonly NuiBind<bool> RestoreButtonEnabledProperty = NuiProperty<bool>.CreateBind(nameof(RestoreButtonEnabledProperty));
        public static readonly NuiButton RestoreButton = new("Przywróć")
        {
            Id = nameof(RestoreButton),
            Width = ApplyCancelButtonWidth,
            Height = ApplyCancelButtonHeight,
            Enabled = RestoreButtonEnabledProperty,
            Padding = 0,
            Margin = 0
        };

        public static readonly NuiButton CancelButton = new("Wyjdź")
        {
            Id = nameof(CancelButton),
            Width = ApplyCancelButtonWidth,
            Height = ApplyCancelButtonHeight,
            Padding = 0,
            Margin = 0
        };
        #endregion

        #region  Arrow Buttons
        public static readonly NuiBind<bool> ArrowButtonRightEnabledProperty = NuiProperty<bool>.CreateBind(nameof(ArrowButtonRightEnabledProperty));
        public static readonly NuiBind<bool> ArrowButtonLeftEnabledProperty = NuiProperty<bool>.CreateBind(nameof(ArrowButtonLeftEnabledProperty));

        public static readonly NuiButtonImage ArrowButtonRight = new("")
        {
            Id = nameof(ArrowButtonRight),
            Height = ArrowBtnHeight,
            Width = ArrowBtnWidth,
            Enabled = ArrowButtonRightEnabledProperty,
            DrawList = new() { new NuiDrawListImage("nui_cnt_right", new NuiRect(4, 4, 1, 1)) },
            Padding = 0,
            Margin = 0
        };
        public static readonly NuiButtonImage ArrowButtonLeft = new("")
        {
            Id = nameof(ArrowButtonLeft),
            Height = ArrowBtnHeight,
            Width = ArrowBtnWidth,
            Enabled = ArrowButtonLeftEnabledProperty,
            DrawList = new() { new NuiDrawListImage("nui_cnt_left", new NuiRect(4, 4, 1, 1)) },
            Padding = 0,
            Margin = 0
        };
        #endregion

        #region Symmetry
        public static readonly NuiBind<bool> SymmetryButtonEnabledProperty = NuiProperty<bool>.CreateBind(nameof(SymmetryButtonEnabledProperty));
        public static readonly NuiBind<bool> SymmetryButtonVisibleProperty = NuiProperty<bool>.CreateBind(nameof(SymmetryButtonVisibleProperty));
        public static readonly NuiBind<string> SymmetryButtonTooltipProperty = NuiProperty<string>.CreateBind(nameof(SymmetryButtonTooltipProperty));
        public static readonly NuiButton SymmetryButton = new("Symetria")
        {
            Id = nameof(SymmetryButton),
            Width = SymmetryButtonWidth,
            Height = SymmetryButtonHeight,
            Enabled = SymmetryButtonEnabledProperty,
            Visible = SymmetryButtonVisibleProperty,
            Tooltip = SymmetryButtonTooltipProperty,
            Padding = 0,
            Margin = 0
        };
        #endregion

        #region Left/Right side buttons
        public static readonly NuiBind<bool> LRSideButtonsVisibleProperty = NuiProperty<bool>.CreateBind(nameof(LRSideButtonsVisibleProperty));

        public static readonly NuiBind<bool> LeftSideButtonEnabledProperty = NuiProperty<bool>.CreateBind(nameof(LeftSideButtonEnabledProperty));
        public static readonly NuiBind<bool> LeftSideButtonSelectedProperty = NuiProperty<bool>.CreateBind(nameof(LeftSideButtonSelectedProperty));
        public static readonly NuiBind<string> LeftSideButtonLabelProperty = NuiProperty<string>.CreateBind(nameof(LeftSideButtonLabelProperty));

        public static readonly NuiBind<bool> RightSideButtonEnabledProperty = NuiProperty<bool>.CreateBind(nameof(RightSideButtonEnabledProperty));
        public static readonly NuiBind<bool> RightSideButtonSelectedProperty = NuiProperty<bool>.CreateBind(nameof(RightSideButtonSelectedProperty));
        public static readonly NuiBind<string> RightSideButtonLabelProperty = NuiProperty<string>.CreateBind(nameof(RightSideButtonLabelProperty));

        public static readonly NuiButtonSelect LeftSideButton = new(LeftSideButtonLabelProperty,LeftSideButtonSelectedProperty)
        {
            Id = nameof(LeftSideButton),
            Width = LRSideButtonWidth,
            Height = LRSideButtonHeight,
            Enabled = LeftSideButtonEnabledProperty,
            Padding = 0,
            Margin = 0
        };
        public static readonly NuiButtonSelect RightSideButton = new(RightSideButtonLabelProperty,RightSideButtonSelectedProperty)
        {
            Id = nameof(RightSideButton),
            Width = LRSideButtonWidth,
            Height = LRSideButtonHeight,
            Enabled = RightSideButtonEnabledProperty,
            Padding = 0,
            Margin = 0
        };
        #endregion

        #region Phenotype

        public static readonly NuiBind<bool> PhenotypesVisibleProperty = NuiProperty<bool>.CreateBind(nameof(PhenotypesVisibleProperty));
        public static readonly NuiBind<bool> Phenotype1SelectedProperty = NuiProperty<bool>.CreateBind(nameof(Phenotype1SelectedProperty));
        public static readonly NuiBind<bool> Phenotype1EnabledProperty = NuiProperty<bool>.CreateBind(nameof(Phenotype1EnabledProperty));
        public static readonly NuiBind<bool> Phenotype2SelectedProperty = NuiProperty<bool>.CreateBind(nameof(Phenotype2SelectedProperty));
        public static readonly NuiBind<bool> Phenotype2EnabledProperty = NuiProperty<bool>.CreateBind(nameof(Phenotype2EnabledProperty));

        public static readonly NuiLabel PhenotypeLabel = new(" Sylwetka")
        {
            Id = nameof(PhenotypeLabel),
            Width = PhenotypeLabelWidth,
            Height = PhenotypeLabelHeight,
            Padding = 0,
            Margin = 0
        };
        
        public static readonly NuiCheck Phenotype1CheckBox = new("Szczupła", Phenotype1SelectedProperty)
        {
            Id = nameof(Phenotype1CheckBox),
            Width = PhenotypeCheckBoxWidth,
            Height = PhenotypeCheckBoxHeight,
            Selected = Phenotype1SelectedProperty,
            Enabled = Phenotype1EnabledProperty,
            Padding = 0,
            Margin = 0
        };
        public static readonly NuiCheck Phenotype2CheckBox = new("Otyła", Phenotype2SelectedProperty)
        {
            Id = nameof(Phenotype2CheckBox),
            Width = PhenotypeCheckBoxWidth,
            Height = PhenotypeCheckBoxHeight,
            Selected = Phenotype2SelectedProperty,
            Enabled = Phenotype2EnabledProperty,
            Padding = 0,
            Margin = 0
        };
        #endregion

        #region Combos
        public static readonly NuiBind<List<NuiComboEntry>> SlotComboEntriesProperty = NuiProperty<List<NuiComboEntry>>.CreateBind(nameof(SlotComboEntriesProperty));
        public static readonly NuiBind<int> SlotComboSelectedProperty = NuiProperty<int>.CreateBind(nameof(SlotComboSelectedProperty));
        public static readonly NuiBind<bool> SlotComboEnabledProperty = NuiProperty<bool>.CreateBind(nameof(SlotComboEnabledProperty));

        public static readonly NuiBind<List<NuiComboEntry>> ValueComboEntriesProperty = NuiProperty<List<NuiComboEntry>>.CreateBind(nameof(ValueComboEntriesProperty));
        public static readonly NuiBind<int> ValueComboSelectedProperty = NuiProperty<int>.CreateBind(nameof(ValueComboSelectedProperty));
        public static readonly NuiBind<bool> ValueComboEnabledProperty = NuiProperty<bool>.CreateBind(nameof(ValueComboEnabledProperty));

        public static readonly NuiCombo SlotCombo = new()
        {
            Id = nameof(SlotCombo),
            Width = SlotComboWidth,
            Height = SlotComboHeight,
            Entries = SlotComboEntriesProperty,
            Selected = SlotComboSelectedProperty,
            Enabled = SlotComboEnabledProperty,
            Padding = 0,
            Margin = 0
        };

        public static readonly NuiCombo ValueCombo = new()
        {
            Id = nameof(ValueCombo),
            Width = ValueComboWidth,
            Height = ValueComboHeight,
            Entries = ValueComboEntriesProperty,
            Selected = ValueComboSelectedProperty,
            Enabled = ValueComboEnabledProperty,
            Padding = 0,
            Margin = 0
        };

        #endregion

        #region Color Image Buttons
        public static readonly NuiBind<string>[] ColorImageResRefProperties;
        public static readonly NuiBind<bool>[] ColorImageEnabledProperties;
        public static readonly NuiBind<bool>[] ColorImageVisibleProperties;
        public static readonly NuiBind<bool>[] ColorImageEncouragedProperties;
        public static readonly NuiBind<NuiRect>[] ColorImageRectProperties;
        public static readonly NuiBind<string>[] ColorImageTooltipProperties;
        public static readonly NuiImage[] ColorImages;
        #endregion

        #region Color Palette Buttons
        public static readonly NuiBind<string> ColorPaletteResRefProperty = NuiProperty<string>.CreateBind(nameof(ColorPaletteResRefProperty));
        public static readonly NuiBind<bool> ColorPaletteVisibleProperty = NuiProperty<bool>.CreateBind(nameof(ColorPaletteVisibleProperty));
        public static readonly NuiImage[] ColorPaletteImages;
        #endregion

        #region BodyHeight Slider
        public static readonly NuiBind<float> BodyHeightSliderValueProperty = NuiProperty<float>.CreateBind(nameof(BodyHeightSliderValueProperty));
        public static readonly NuiBind<float> BodyHeightSliderMinProperty = NuiProperty<float>.CreateBind(nameof(BodyHeightSliderMinProperty));
        public static readonly NuiBind<float> BodyHeightSliderMaxProperty = NuiProperty<float>.CreateBind(nameof(BodyHeightSliderMaxProperty));
        public static readonly NuiBind<bool> BodyHeightSliderVisibleProperty = NuiProperty<bool>.CreateBind(nameof(BodyHeightSliderVisibleProperty));
        public static readonly NuiSliderFloat BodyHeightSlider = new(BodyHeightSliderValueProperty, BodyHeightSliderMinProperty, BodyHeightSliderMaxProperty)
        {
            Id = nameof(BodyHeightSlider),
            Height = BodyHeightSliderHeight,
            Width = BodyHeightSliderWidth,
            Visible = BodyHeightSliderVisibleProperty
        };

        public static readonly NuiLabel BodyHeightSliderLabel = new("Wzrost:"){Height = PhenotypeLabelHeight, Width = PhenotypeLabelWidth};

        #endregion

        #region NUIWindow
        public static readonly NuiWindow Window;

        static AppearanceEditorView()
        {

            ColorImageResRefProperties = new NuiBind<string>[6];
            ColorImageVisibleProperties = new NuiBind<bool>[6];
            ColorImageEnabledProperties = new NuiBind<bool>[6];
            ColorImageEncouragedProperties = new NuiBind<bool>[6];
            ColorImageRectProperties = new NuiBind<NuiRect>[6];
            ColorImageTooltipProperties = new NuiBind<string>[6];
            ColorImages = new NuiImage[6];

            for(int i = 0; i < 6; i++)
            {
                var strIdx = i.ToString();

                ColorImageResRefProperties[i] = NuiProperty<string>.CreateBind("ColBtnResRef" + strIdx);
                ColorImageVisibleProperties[i] = NuiProperty<bool>.CreateBind("ColBtnVisible" + strIdx);
                ColorImageEnabledProperties[i] = NuiProperty<bool>.CreateBind("ColBtnEnabled" + strIdx);
                ColorImageEncouragedProperties[i] = NuiProperty<bool>.CreateBind("ColBtnEncouraged" + strIdx);
                ColorImageRectProperties[i] = NuiProperty<NuiRect>.CreateBind("ColBtnRect" + strIdx);
                ColorImageTooltipProperties[i] = NuiProperty<string>.CreateBind("ColBtnTtip" + strIdx);

                ColorImages[i] = new(ColorImageResRefProperties[i])
                {
                    Id = "ColImg" + strIdx,
                    Enabled = ColorImageEnabledProperties[i],
                    Visible = ColorImageVisibleProperties[i],
                    Width = ColorImageWidth,
                    Height = ColorImageHeight,
                    ImageRegion = ColorImageRectProperties[i],
                    Tooltip = ColorImageTooltipProperties[i],
                    DisabledTooltip = ColorImageTooltipProperties[i],
                    Encouraged = ColorImageEncouragedProperties[i],
                    Margin = 0,
                    Padding = 1,
                    ImageAspect = NuiAspect.Fill
                };
            }

            ColorPaletteImages = new NuiImage[16*11];

            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    var suffix = $"_{i}_{j}";

                    ColorPaletteImages[i * 16 + j] = new NuiImage(ColorPaletteResRefProperty)
                    {
                        Id = "ColPalImg" + suffix,
                        Width = 20,
                        Height = 20,
                        Margin = 0,
                        Padding = 1,
                        ImageAspect = NuiAspect.Fill,
                        ImageRegion = NuiProperty<NuiRect>.CreateBind("CP_Rect"+suffix),
                        Visible = ColorPaletteVisibleProperty,
                        Enabled = NuiProperty<bool>.CreateBind("CP_Enabled" + suffix),
                        Encouraged = NuiProperty<bool>.CreateBind("CP_Encouraged" + suffix)
                    };
                }
            }

            var colorPaletteRows = new NuiRow[11];

            for(int i = 0; i < 11; i++)
            {
                colorPaletteRows[i] = new NuiRow { Children = ColorPaletteImages[(i * 16)..(i * 16 + 16)].Cast<NuiElement>().ToList(),
                Padding = 0,
                Margin = 0  };
            }

            var generalButtonsRow = new NuiRow() { Children = new() { GeneralButtonBody, GeneralButtonArmor, GeneralButtonWeapon },
            Padding = 0,
            Margin = 0 };



            var slotAndSymmCol = new NuiColumn()
            {
                Children = new() { SlotCombo, new NuiSpacer() { Height = 10, Margin = 0, Padding = 0 }, SymmetryButton },
                Padding = 0,
                Margin = 0
            };

            var valAndColorCol = new NuiColumn()
            {
                Children = new()
                {                    
                    new NuiRow(){Children = new (){ new NuiSpacer() { Width = 45, Margin = 0, Padding = 0 }, ColorImages[0],ColorImages[2],ColorImages[4]},
                        Padding = 0,
                        Margin = 0},
                    new NuiRow(){Children = new (){ new NuiSpacer() { Width = 45, Margin = 0, Padding = 0 }, ColorImages[1],ColorImages[3],ColorImages[5]},
                        Padding = 0,
                        Margin = 0},
                    new NuiRow(){
                        Children = new(){ArrowButtonLeft,ValueCombo,ArrowButtonRight},
                        Padding = 0,
                        Margin = 0
                    },
                }
            };

            var combosRow = new NuiRow() { Children = new() {
                slotAndSymmCol,
                valAndColorCol
                },
            Padding = 0,
            Margin = 0 };

            var leftRightButtonsRow = new NuiRow() {
                Children = new()
                {
                    new NuiSpacer(){Width = 25, Margin = 0, Padding = 0},
                    LeftSideButton,
                    RightSideButton
                },
                Padding = 0,
                Margin = 0,
                Visible = LRSideButtonsVisibleProperty
            };

            var phenoApplyCancelButtonsRow = new NuiRow()
            {
                Children = new()
                {

                    new NuiColumn(){Children = new(){new NuiSpacer() {Width = 5, Padding = 0, Margin = 0}, ApplyButton, RestoreButton, CancelButton},
                        Padding = 0,
                        Margin = 0
                    },
                    new NuiSpacer(){Width = 5, Margin = 0,Padding = 0},
                    new NuiColumn(){ Children = new() { BodyHeightSliderLabel, BodyHeightSlider, PhenotypeLabel, Phenotype1CheckBox, Phenotype2CheckBox },
                        Padding = 0,
                        Margin = 0,
                        Visible = PhenotypesVisibleProperty
                    },
                },
                Padding = 0,
                Margin = 0
            };

            var mainColumn = new NuiColumn()
            {
                Children = new()
                {
                    generalButtonsRow,
                    new NuiSpacer(){Height = 5, Margin = 0, Padding = 0},
                    combosRow,
                    new NuiSpacer(){Height = 10, Margin = 0, Padding = 0},
                    leftRightButtonsRow,
                    new NuiSpacer(){Height = 5, Margin = 0, Padding = 0},
                    phenoApplyCancelButtonsRow,
                    new NuiSpacer(){Height = 20, Margin = 0, Padding = 0}
                },
            Padding = 0,
            Margin = 0
            };

            mainColumn.Children.AddRange(colorPaletteRows);

            Window = new NuiWindow(mainColumn, "Edytor wyglądu")
            {
                Id = "AppearanceEditor",
                Geometry = NuiProperty<NuiRect>.CreateBind("WindowGeometry"),
                Resizable = false,
                Closable = true,
                Border = false,
                Transparent = true
            };

        }
        
        #endregion
    }
}