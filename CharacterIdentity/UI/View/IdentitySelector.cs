using System;
using System.Collections.Generic;
using System.Text;
using Anvil.API;
using ExtensionsPlugin;

//using ExtensionsPlugin;



namespace CharacterIdentity.UI.View
{
    internal static class IdentitySelector
    {
        public static readonly NuiWindow NuiWindow;

        #region Properties
        public static readonly NuiBind<bool> NewBtnEnabledProperty = NuiProperty<bool>.CreateBind(nameof(NewBtnEnabledProperty));
        public static readonly NuiBind<string> NewBtnTooltipProperty = NuiProperty<string>.CreateBind(nameof(NewBtnTooltipProperty));
        public static readonly NuiBind<string> NewBtnDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(NewBtnDisabledTooltipProperty));
        public static readonly NuiBind<string> PickButtonDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(PickButtonDisabledTooltipProperty));

        public static readonly NuiBind<bool> EditBtnsEnabledProperty = NuiProperty<bool>.CreateBind(nameof(EditBtnsEnabledProperty));
        public static readonly NuiBind<string> EditBtnDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(EditBtnDisabledTooltipProperty));
        public static readonly NuiBind<string> DeleteBtnDisabledTooltipProperty = NuiProperty<string>.CreateBind(nameof(DeleteBtnDisabledTooltipProperty));

        public static readonly NuiBind<bool> PickBtnEnabledProperty = NuiProperty<bool>.CreateBind(nameof(PickBtnEnabledProperty));
        public static readonly NuiBind<bool> RestoreBtnEnabledProperty = NuiProperty<bool>.CreateBind(nameof(RestoreBtnEnabledProperty));

        public static readonly NuiBind<List<NuiComboEntry>> ComboEntriesProperty = NuiProperty<List<NuiComboEntry>>.CreateBind(nameof(ComboEntriesProperty));
        public static readonly NuiBind<int> ComboSelectionProperty = NuiProperty<int>.CreateBind(nameof(ComboSelectionProperty));
        public static readonly NuiBind<bool> ComboEnabledProperty = NuiProperty<bool>.CreateBind(nameof(ComboEnabledProperty));

        public static readonly NuiBind<string> PortraitResRefProperty = NuiProperty<string>.CreateBind(nameof(PortraitResRefProperty));
        public static readonly NuiBind<bool> PortraitEnabledProperty = NuiProperty<bool>.CreateBind(nameof(PortraitEnabledProperty));
        #endregion


        #region Constants
        const float PortraitW = 128 + 11.5f;
        const float PortraitH = 200 + 11.5f;
        const float EditBtnW = 100;
        const float EditBtnH = (PortraitH - 10) / 3;
        const float PickerW = PortraitW + EditBtnW;
        const float PickerH = 30;
        const float ControlBtnH = 37.5f;
        #endregion


        #region Elements

        #region Buttons
        public static readonly NuiButton NewButton = new("Stwórz")
        {
            Id = nameof(NewButton),
            Width = EditBtnW,
            Height = EditBtnH,
            Enabled = NewBtnEnabledProperty,
            DisabledTooltip = NewBtnDisabledTooltipProperty,
            Tooltip = NewBtnTooltipProperty
        };
        public static readonly NuiButton EditButton = new("Edytuj")
        {
            Id = nameof(EditButton),
            Width = EditBtnW,
            Height = EditBtnH,
            Enabled = EditBtnsEnabledProperty,
            DisabledTooltip = EditBtnDisabledTooltipProperty
        };
        public static readonly NuiButton DeleteButton = new("Usuń")
        {
            Id = nameof(DeleteButton),
            Width = EditBtnW,
            Height = EditBtnH,
            Enabled = EditBtnsEnabledProperty,
            ForegroundColor = ColorConstants.Red,
            Tooltip = "To nieodwracalne!",
            DisabledTooltip = DeleteBtnDisabledTooltipProperty
        };
        public static readonly NuiButton PickButton = new("Przybierz fałszywą tożsamość")
        {
            Id = nameof(PickButton),
            Width = PickerW + 6,
            Height = ControlBtnH,
            Enabled = PickBtnEnabledProperty,
            DisabledTooltip = PickButtonDisabledTooltipProperty,
            Tooltip = "Upewnij się, że nikt nie patrzy!"
        };

        public static readonly NuiButton RestoreButton = new("Przywróć prawdziwą tożsamość")
        {
            Id = nameof(RestoreButton),
            Width = PickerW + 6,
            Height = ControlBtnH,
            Enabled = RestoreBtnEnabledProperty,
            DisabledTooltip = "Jesteś teraz sobą.",
            Tooltip = PickButton.Tooltip
        };
        #endregion

        public static readonly NuiCombo PickerCombo = new()
        {
            Id = nameof(PickerCombo),
            Selected = ComboSelectionProperty,
            Entries = ComboEntriesProperty,
            Enabled = ComboEnabledProperty,
            Width = PickerW + 8.5f,
            Height = PickerH
        };
        public static readonly NuiImage PortraitImage = new(PortraitResRefProperty)
        {
            Id = nameof(PortraitImage),
            Enabled = PortraitEnabledProperty,
            Width = 128,
            Height = 200
        };
        #endregion


        static IdentitySelector()
        {
            var mainGrp = new NuiGroup();
            var mainCol = new NuiColumn();

            mainGrp.Layout = mainCol;
            mainGrp.Border = false;
            mainGrp.Scrollbars = NuiScrollbars.None;

            var rowA = new NuiRow();

            var colAA = new NuiColumn();
            colAA.Children.Add(PortraitImage);

            var grpA = new NuiGroup()
            {
                Border = true,
                Layout = colAA,
                Scrollbars = NuiScrollbars.None,
                Height = PortraitH,
                Width = PortraitW
            };

            var colAB = new NuiColumn();
            colAB.Children.AddRange(new NuiElement[] { NewButton, EditButton, DeleteButton });

            rowA.Children.AddRange(new NuiElement[] { grpA, colAB });

            var colB = new NuiColumn();
            colB.Children.AddRange(new NuiElement[] { PickButton, RestoreButton });
            mainCol.Children.AddRange(new NuiElement[] { rowA, PickerCombo, colB });

            var mainW = PortraitW + EditBtnW;
            var mainH = PortraitH + PickerH + 2 * ControlBtnH;

            mainCol.Width = mainW;
            mainCol.Height = mainH;


            NuiWindow = new NuiWindow(mainGrp, "Twoje tożsamości")
            {
                Id = nameof(IdentitySelector),
                Border = true,
                Closable = true,
                Resizable = false,
                Geometry = new NuiRect(-1, 200, mainW + 30, mainH + 75)
            };
        }
    }
}