using Anvil.API;

namespace CharacterIdentity.UI.View
{
    /// <summary>
    ///  TODO: implement appearance edit
    /// </summary>
    internal static class IdentityEditor
    {
        public static readonly NuiWindow NuiWindow;

        
        #region Properties
        public static readonly NuiBind<string> PortraitProperty = NuiProperty<string>.CreateBind(nameof(PortraitProperty));

        public static readonly NuiBind<string> FirstNameProperty = NuiProperty<string>.CreateBind(nameof(FirstNameProperty));
        public static readonly NuiBind<Color> FirstNameLabelColorProperty = NuiProperty<Color>.CreateBind(nameof(FirstNameLabelColorProperty));
        public static readonly NuiBind<bool> FirstNameEncouragedProperty = NuiProperty<bool>.CreateBind(nameof(FirstNameEncouragedProperty));

        public static readonly NuiBind<string> LastNameProperty = NuiProperty<string>.CreateBind(nameof(LastNameProperty));
        public static readonly NuiBind<Color> LastNameLabelColorProperty = NuiProperty<Color>.CreateBind(nameof(LastNameLabelColorProperty));

        public static readonly NuiBind<string> NameCharactersCountProperty = NuiProperty<string>.CreateBind(nameof(NameCharactersCountProperty));

        public static readonly NuiBind<string> DescriptionProperty = NuiProperty<string>.CreateBind(nameof(DescriptionProperty));

        public static readonly NuiBind<int> MinAgeProperty = NuiProperty<int>.CreateBind(nameof(MinAgeProperty));
        public static readonly NuiBind<int> AgeProperty = NuiProperty<int>.CreateBind(nameof(AgeProperty));
        public static readonly NuiBind<int> MaxAgeProperty = NuiProperty<int>.CreateBind(nameof(MaxAgeProperty));
        public static readonly NuiBind<bool> LowerAgeBtnEnabledProperty = NuiProperty<bool>.CreateBind(nameof(LowerAgeBtnEnabledProperty));
        public static readonly NuiBind<bool> RaiseAgeBtnEnabledProperty = NuiProperty<bool>.CreateBind(nameof(RaiseAgeBtnEnabledProperty));

        public static readonly NuiBind<string> AgeLabelProperty = NuiProperty<string>.CreateBind(nameof(AgeLabelProperty));

        public static readonly NuiBind<bool> ApplyBtnEnabledProperty = NuiProperty<bool>.CreateBind(nameof(ApplyBtnEnabledProperty));
        public static readonly NuiBind<bool> ApplyBtnEncouragedProperty = NuiProperty<bool>.CreateBind(nameof(ApplyBtnEncouragedProperty));
        private static readonly NuiValue<string> _subDataEditorsDisabledTooltip = NuiProperty<string>.CreateValue("Wprowadź prawidłowe imię.");

        public static readonly NuiBind<string> DescCharactersCountLabelProperty = NuiProperty<string>.CreateBind(nameof(DescCharactersCountLabelProperty));
        public static readonly NuiBind<Color> DescCharactersCountColorProperty = NuiProperty<Color>.CreateBind(nameof(DescCharactersCountColorProperty));

        public static readonly NuiBind<string> WindowTitleProperty = NuiProperty<string>.CreateBind(nameof(WindowTitleProperty));
        public static readonly NuiBind<bool> WindowAcceptsInputProperty = NuiProperty<bool>.CreateBind(nameof(WindowAcceptsInputProperty));

        public static readonly NuiBind<bool> MaleCheckboxSelectedProperty = NuiProperty<bool>.CreateBind(nameof(MaleCheckboxSelectedProperty));
        public static readonly NuiBind<bool> FemaleCheckboxSelectedProperty = NuiProperty<bool>.CreateBind(nameof(FemaleCheckboxSelectedProperty));
        
        public static readonly NuiBind<bool> MaleCheckboxEnabledProperty = NuiProperty<bool>.CreateBind(nameof(MaleCheckboxEnabledProperty));
        public static readonly NuiBind<bool> FemaleCheckboxEnabledProperty = NuiProperty<bool>.CreateBind(nameof(FemaleCheckboxEnabledProperty));

        public static readonly NuiBind<string> GenderCheckboxDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(GenderCheckboxDisabledTooltipProperty));
        #endregion
        
        
        #region Constants
        private const int PortraitW = 128;
        private const int PortraitH = 200;
        private const int ControlBtnH = 50;
        private const int ControlBtnW = PortraitW + 50;
        private const int WindowW = ControlBtnW * 2 + 200;
        private const int SliderW = WindowW - PortraitW - 60;
        private const int AgeLabelW = WindowW - PortraitW - 30;
        private const int LabelH = 50;
        private const int DescH = 300;
        #endregion


        #region Elements

        #region Buttons
        public static readonly NuiButton ApplyButton = new("Zatwierdź")
        {
            Id = nameof(ApplyButton),
            Enabled = ApplyBtnEnabledProperty,
            Width = ControlBtnW,
            Height = ControlBtnH,
            DisabledTooltip = _subDataEditorsDisabledTooltip,
            Encouraged = ApplyBtnEncouragedProperty
        };
        public static readonly NuiButton AbortButton = new("Anuluj")
        {
            Id = nameof(AbortButton),
            Width = ControlBtnW,
            Height = ControlBtnH
        };

        public static readonly NuiButton RaiseAgeButton = new("")
        {
            Id = nameof(RaiseAgeButton),
            Height = 20,
            Width = 20,
            Enabled = RaiseAgeBtnEnabledProperty,
            DrawList = new() { new NuiDrawListImage("nui_cnt_right", new NuiRect(4, 4, 1, 1)) },
        };
        public static readonly NuiButton LowerAgeButton = new("")
        {
            Id = nameof(LowerAgeButton),
            Height = 20,
            Width = 20,
            Enabled = LowerAgeBtnEnabledProperty,
            DrawList = new() { new NuiDrawListImage("nui_cnt_left", new NuiRect(4, 4, 1, 1)) },
        };

        public static readonly NuiImage PortraitImage = new(PortraitProperty)
        {
            Id = nameof(PortraitImage),
            Width = PortraitW,
            Height = PortraitH,
            Enabled = ApplyBtnEnabledProperty,
            DisabledTooltip = _subDataEditorsDisabledTooltip,
            Tooltip = "Kliknij, aby wybrać portret."
        };

        public static readonly NuiButton AppearanceButton = new("Wygląd")
        {
            Id = nameof(AppearanceButton),

            Enabled = ApplyBtnEnabledProperty,
            Tooltip = "Uwaga! Przybierzesz fałszywą tożsamość, której wygląd chcesz zedytować.",
            DisabledTooltip = _subDataEditorsDisabledTooltip,
            ForegroundColor = ColorConstants.Red,
            Height = LabelH / 2,
            Width = 100
        };
        #endregion
        
        #region TextEdits
        public static readonly NuiTextEdit FirstNameTextEdit = new("Imię", FirstNameProperty, CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters, false)
        {
            Id = nameof(FirstNameTextEdit),
            Enabled = true,
            Encouraged = FirstNameEncouragedProperty,
            WordWrap = false,
            Width = SliderW - 40
        };
        public static readonly NuiTextEdit LastNameTextEdit = new("Nazwisko", LastNameProperty, (ushort)(CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters - CharacterIdentityService.IdentityEditorConfig.MinimumNameCharacters), false)
        {
            Id = nameof(LastNameTextEdit),
            Enabled = ApplyBtnEnabledProperty,
            DisabledTooltip = _subDataEditorsDisabledTooltip,
            WordWrap = false,
            Width = SliderW - 40
        };
        public static readonly NuiTextEdit DescriptionTextEdit = new("Opis", DescriptionProperty, CharacterIdentityService.IdentityEditorConfig.MaximumDescriptionCharacters, true)
        {
            Id = nameof(DescriptionTextEdit),
            Enabled = ApplyBtnEnabledProperty,
            DisabledTooltip = _subDataEditorsDisabledTooltip,
            WordWrap = true,
            Height = DescH - 5,
            Width = WindowW
        };
        #endregion
        
        #region Labels
        public static readonly NuiLabel AgeLabel = new(AgeLabelProperty)
        {
            Width = AgeLabelW,
            Height = 20,
            VerticalAlign = NuiVAlign.Bottom
        };
        public static readonly NuiLabel NameCharactersCountLabel = new(NameCharactersCountProperty)
        {
            HorizontalAlign = NuiHAlign.Right,
            Height = 20,
            Width = 200
        };
        public static readonly NuiLabel DescriptionCharactersCountLabel = new(DescCharactersCountLabelProperty)
        {
            ForegroundColor = DescCharactersCountColorProperty,
            HorizontalAlign = NuiHAlign.Right,
            Height = LabelH
        };
        public static readonly NuiLabel GenderLabel = new("Płeć:")
        {
          Height = 20,
          Width = 100
        };
        #endregion

        #region checkboxes
        public static readonly NuiCheck MaleCheckbox = new("Mężczyzna", MaleCheckboxSelectedProperty)
        {
            Enabled = MaleCheckboxEnabledProperty,
            DisabledTooltip = GenderCheckboxDisabledTooltipProperty,
            Width = 150,
            Height = 20
        };
        public static readonly NuiCheck FemaleCheckbox = new("Kobieta", FemaleCheckboxSelectedProperty)
        {
            Enabled = FemaleCheckboxEnabledProperty,
            DisabledTooltip = GenderCheckboxDisabledTooltipProperty,
            Width = 150,
            Height = 20
        };
        #endregion

        public static readonly NuiSlider AgeSlider = new(AgeProperty, MinAgeProperty, MaxAgeProperty)
        {
            Id = nameof(AgeSlider),
            Width = SliderW - 40,
            Height = 20,
            DisabledTooltip = _subDataEditorsDisabledTooltip,
            Enabled = ApplyBtnEnabledProperty,
        };
        #endregion
        
        
        static IdentityEditor()
        {

            var mainCol = new NuiColumn();

            var infoCol = new NuiColumn();

            var sliderRow = new NuiRow();
            sliderRow.Children.AddRange(new NuiElement[] { LowerAgeButton, AgeSlider, RaiseAgeButton });

            var subInfoRowA = new NuiRow();
            subInfoRowA.Children.AddRange(new NuiElement[] { AppearanceButton, NameCharactersCountLabel });

            var genderRow = new NuiRow() { Children = new() { GenderLabel, MaleCheckbox, FemaleCheckbox } };

            infoCol.Children.AddRange(new NuiElement[] { FirstNameTextEdit, LastNameTextEdit, subInfoRowA, genderRow, AgeLabel, sliderRow });


            var infoRow = new NuiRow();
            var portraitGroup = new NuiGroup()
            {
                Layout = new NuiColumn()
                {
                    Children = new() { PortraitImage }
                },
                Border = true,
                Scrollbars = NuiScrollbars.None,
                Width = PortraitW,
                Height = PortraitH
            };

            infoRow.Children.AddRange(new NuiElement[] { portraitGroup, infoCol });

            var separatorL = new NuiSpacer() { Width = (WindowW - ControlBtnW * 2) / 2 - 10, Height = ControlBtnH };
            var separatorM = new NuiSpacer() { Width = 10, Height = ControlBtnH };
            var separatorR = new NuiSpacer() { Width = (WindowW - ControlBtnW * 2) / 2 - 10, Height = ControlBtnH };
            var controlButtonsRow = new NuiRow();
            controlButtonsRow.Children.AddRange(new NuiElement[] { separatorL, AbortButton, separatorM, ApplyButton, separatorR });

            mainCol.Children.AddRange(new NuiElement[] { infoRow, DescriptionTextEdit, DescriptionCharactersCountLabel, controlButtonsRow });

            mainCol.Visible = WindowAcceptsInputProperty;

            NuiWindow = new NuiWindow(mainCol, WindowTitleProperty)
            {
                Id = nameof(IdentityEditor),
                Resizable = false,
                Closable = true,
                Border = true,
                AcceptsInput = WindowAcceptsInputProperty,
                Geometry = new NuiRect(-1, -1, ControlBtnW * 2 + 226, PortraitH + DescH + ControlBtnH + LabelH + 72)
            };
        }
    }
}