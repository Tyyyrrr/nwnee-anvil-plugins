using Anvil.API;
using NuiMVC;

using ExtensionsPlugin;

using PPModel = CharacterIdentity.UI.Model.PortraitPicker;
using PPView = CharacterIdentity.UI.View.PortraitPicker;

using System.Linq;

namespace CharacterIdentity.UI.Controller
{
    internal sealed class PortraitPicker : ControllerBase
    {
        private readonly PPModel _model;

        private readonly string _defaultPortrait;
        private readonly string _initialPortrait;


        public PortraitPicker(NwPlayer player, PortraitStorageService portraitStorage, string? initialPortrait = null, Gender overrideGender = Gender.None) : base(player, PPView.NuiWindow)
        {
            _defaultPortrait = player.ControlledCreature!.GetDefaultPortraitResRef_Large();

            _initialPortrait = string.IsNullOrEmpty(initialPortrait) ? _defaultPortrait : initialPortrait;

            var portraits = portraitStorage.GetPortraitsForCreature(player.ControlledCreature!, overrideGender);

            _model = new(portraits, PPView.ColumnCount);
            _model.SelectPortrait(_initialPortrait);

            int rows = _model.RowCount;

            SetValue(PPView.RowCountProperty, rows);
            SetValue(PPView.BigImageProperty, _model.SelectedPortrait!);

            for (int i = 0; i < PPView.ColumnCount; i++)
            {
                var col = _model.GetColumn(i);
                var property = PPView.GetProperty(i);
                SetValues(property, col.ToArray());
            }

        }
        


        protected override void OnMouseDown(string elementId, int arrayIndex)
        {
            int col = int.Parse(elementId);
            int row = arrayIndex;

            _model.SelectPortrait(col, row);

            SetValue(PPView.BigImageProperty, _model.SelectedPortrait!);
        }

        protected override object? OnClose()
        {
            string str;
            if (applying)
                str = _model.SelectedPortrait ?? _defaultPortrait;
            else str = _initialPortrait;
            return str[..(str.Length - 1)];
        }

        bool applying = false;
        protected override void OnClick(string elementId)
        {
            if (elementId == nameof(PPView.OkButton))
            {
                applying = true;
                Close();
                applying = false;
            }
            else
            {
                _model.SelectPortrait(_initialPortrait);
                Close();
            }
        }
    }
}