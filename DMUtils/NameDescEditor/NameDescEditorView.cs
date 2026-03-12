using System.Linq;
using Anvil.API;

namespace DMUtils.NameDescEditor
{
    internal static class NameDescEditorView
    {
        public static readonly NuiWindow NuiWindow;

        public static readonly NuiBind<string> NameTextEditValueProperty = NuiProperty<string>.CreateBind(nameof(NameTextEditValueProperty));
        public static readonly NuiBind<string> DescriptionTextEditValueProperty = NuiProperty<string>.CreateBind(nameof(DescriptionTextEditValueProperty));

        private static readonly NuiLabel NameTextLabel = new("Nazwa:")
        {
            Height=40,
            Width=80
        };
        private static readonly NuiLabel DescriptionTextLabel = new("Opis:")
        {
            Height=40,
            Width=80
        };

        public static readonly NuiTextEdit NameTextEdit = new(nameof(NameTextEdit), NameTextEditValueProperty,64, false)
        {
            Height=40,
            Width=220,
        };
        public static readonly NuiTextEdit DescriptionTextEdit = new(nameof(DescriptionTextEdit), DescriptionTextEditValueProperty, 5000,true)
        {
            Height=400,
            Width=320,
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

        static NameDescEditorView()
        {

            NuiRow buttonsRow = new()
            {
                Children= new NuiElement[]{OkButton,ExitButton}.ToList()
            };



            NuiColumn mainLayout = new()
            {
                Children= new NuiElement[]{
                    NameTextLabel,
                    NameTextEdit,
                    DescriptionTextLabel,
                    DescriptionTextEdit,
                    buttonsRow
                }.ToList()
            };

            NuiWindow = new(mainLayout, "Zmiana nazwy/opisu")
            {
                Id = nameof(NameDescEditorView),
                Geometry = new NuiRect(-1, 128, 380,450),
                Resizable=false
            };
        }
    }
}