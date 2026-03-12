using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace DMUtils.VFXSpawner
{
    internal static class VFXSpawnerView
    {
        public static readonly NuiWindow NuiWindow;

        public static readonly NuiBind<List<NuiComboEntry>> ComboEntriesProperty = NuiProperty<List<NuiComboEntry>>.CreateBind(nameof(ComboEntriesProperty));
        public static readonly NuiBind<int> SelectedVFXEntryProperty = NuiProperty<int>.CreateBind(nameof(SelectedVFXEntryProperty));        
        public static readonly NuiBind<string> SelectedVFXIndexStringProperty = NuiProperty<string>.CreateBind(nameof(SelectedVFXIndexStringProperty));
        public static readonly NuiBind<string> DurationTextEditValueProperty = NuiProperty<string>.CreateBind(nameof(DurationTextEditValueProperty));
        public static readonly NuiCombo VFXCombo = new()
        {
            Id=nameof(VFXCombo),
            Entries=ComboEntriesProperty,
            Selected=SelectedVFXEntryProperty,
            Height = 40,
            Width = 150
        };

        private static readonly NuiLabel DurationTextLabel = new("Czas trwania:")
        {
            Height=40,
            Width=120
        };

        private static readonly NuiLabel VFXIndexLabel = new("Indeks: ")
        {
            Height=40,
            Width=80
        };
        private static readonly NuiLabel VFXIndexValueLabel = new(SelectedVFXIndexStringProperty)
        {
            Height=40,
            Width=80
        };

        public static readonly NuiTextEdit DurationTextEdit = new(nameof(DurationTextEdit), DurationTextEditValueProperty, 4, false)
        {
            Height=40,
            Width=40,
            Tooltip="-1: trwały efekt, 0: natychmiastowy efekt"
        };

        public static readonly NuiButton OkButton = new("Zatwierdź")
        {
            Id=nameof(OkButton),
            Height=40,
            Width=80
        };
        public static readonly NuiButton ExitButton = new("Anuluj")
        {
            Id=nameof(ExitButton),
            Height=40,
            Width=80
        };

        static VFXSpawnerView()
        {


            NuiRow buttonsRow = new()
            {
                Children= new NuiElement[]{OkButton,ExitButton}.ToList()
            };

            NuiRow appIdRow = new()
            {
                Children = new NuiElement[]{VFXIndexLabel,VFXIndexValueLabel}.ToList()
            };

            NuiRow durationRow = new()
            {
                Children = new NuiElement[] {DurationTextLabel, DurationTextEdit}.ToList()
            };

            NuiColumn mainLayout = new()
            {
                Children= new NuiElement[]{
                    VFXCombo,
                    appIdRow,
                    durationRow,
                    buttonsRow
                }.ToList()
            };

            NuiWindow = new(mainLayout, "Tworzenie VFX")
            {
                Id = nameof(VFXSpawnerView),
                Geometry = new NuiRect(-1, 128, 230,200),
                Resizable=false
            };
        }
    }
}