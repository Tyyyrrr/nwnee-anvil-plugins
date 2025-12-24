using System.Linq;
using Anvil.API;

namespace CharacterIdentity.UI.View
{
    internal static class PortraitPicker
    {
        public static readonly NuiWindow NuiWindow;

        public static readonly NuiBind<int> RowCountProperty = NuiProperty<int>.CreateBind(nameof(RowCountProperty));
        public static readonly NuiBind<string> BigImageProperty = NuiProperty<string>.CreateBind(nameof(BigImageProperty));

        public const int ColumnCount = 5;
        private static readonly NuiElement[] _buttons = new NuiElement[ColumnCount];

        private static readonly NuiImage _bigImage = new(BigImageProperty) { Height = 400, Width = 256 };

        public static readonly NuiButton OkButton = new("Zatwierdź") { Id = nameof(OkButton), Height = 50, Width = 125 };
        public static readonly NuiButton CancelButton = new("Anuluj") { Id = nameof(CancelButton), Height = 50, Width = 125 };

        public static NuiBind<string> GetProperty(int buttonId) => (NuiBind<string>)((NuiImage)((NuiRow)((NuiGroup)_buttons[buttonId]).Layout!).Children[0]).ResRef;


        static PortraitPicker()
        {
            for (int i = 0; i < ColumnCount; i++)
            {
                var property = NuiProperty<string>.CreateBind(i.ToString());

                var row = new NuiRow();

                var img = new NuiImage(property)
                {
                    Id = i.ToString(),
                    Width = 64,
                    Height = 100,
                    ImageAspect = NuiAspect.Fit100
                };
                row.Children.Add(img);
                var grp = new NuiGroup
                {
                    Border = true,
                    Layout = row,
                    Scrollbars = NuiScrollbars.None
                };
                _buttons[i] = grp;
            }

            var mainLayout = new NuiColumn();
            var mainRow = new NuiRow();

            
            var okCancelBtnsRow = new NuiRow();
            okCancelBtnsRow.Children.AddRange(new NuiElement[] { CancelButton, OkButton });
            var firstCol = new NuiColumn();
            firstCol.Children.Add(_bigImage);
            firstCol.Children.Add(okCancelBtnsRow);
            
            var cells = _buttons.Select(b => new NuiListTemplateCell(b)).ToList();

            var list = new NuiList(cells, RowCountProperty)
            {
                RowHeight = 110,
                Border = true,
                Height = 460
            };

            mainRow.Children.Add(firstCol);
            mainRow.Children.Add(list);

            mainLayout.Children.Add(mainRow);

            NuiWindow = new(mainLayout, "Wybór portretu")
            {
                Id = nameof(PortraitPicker),
                Geometry = new NuiRect(-1, 128, ColumnCount * 64 + 70+256 + 64, 515)
            };
        }
    }
}