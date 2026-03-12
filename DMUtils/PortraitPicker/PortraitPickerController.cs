using Anvil.API;
using NuiMVC;

using PPModel = DMUtils.PortraitPicker.PortraitPickerModel;
using PPView = DMUtils.PortraitPicker.PortraitPickerView;

using System.Linq;
using System.Collections.Generic;
using System;


namespace DMUtils.PortraitPicker
{
    internal sealed class PortraitPickerController : ControllerBase
    {
        private readonly PPModel _model;

        private readonly string _defaultPortrait;
        private readonly string _initialPortrait;

        private readonly NwObject _subject;

        private static string[] _creatureResRefs = Array.Empty<string>();
        private static string[] _placeableResRefs = Array.Empty<string>();

        public static void LoadPortraits()
        {
            var creatureList = new List<string>();
            var placeableList = new List<string>();

            foreach(PortraitTableEntry entry in NwGameTables.PortraitTable)
            {
                var brr = entry.BaseResRef;

                if(brr != null && !brr.Contains('*'))
                {
                    if(brr.StartsWith("plc", StringComparison.CurrentCultureIgnoreCase)
                    || brr.StartsWith("tm_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("wit2_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("PX2_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("msc_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("pwc_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("tnp_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("pmw_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("dcp",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("ad_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("stat_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("abp_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("pfs_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("crps_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("boarded",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gibed",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gichair",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gilight",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gicouch",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gishlf",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gicar",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gifpot",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("hfence",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("hvstall",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("hchimn",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("flbed",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("curtain",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("giwindow",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gipict",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("gicolmn",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("lumber_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("trpile",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("uwsh_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("pp_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("otr_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("nw2",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("ccc_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("p_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("dlp_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("z217_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("letter_",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("dcurtain",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("cart",StringComparison.OrdinalIgnoreCase)
                    || brr.StartsWith("banner",StringComparison.OrdinalIgnoreCase))
                        placeableList.Add("po_"+brr);
                    else creatureList.Add("po_"+brr);
                }
            }

            _creatureResRefs = creatureList.ToArray();
            _placeableResRefs = placeableList.ToArray();

        }
        public PortraitPickerController(NwPlayer player, NwCreature subject) : base(player, PPView.NuiWindow)
        {

            _defaultPortrait = subject.PortraitResRef;
            _initialPortrait = _defaultPortrait;
            _subject = subject;

            _model = new(_creatureResRefs, PPView.ColumnCount);
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
        
        public PortraitPickerController(NwPlayer player, NwPlaceable subject) : base(player, PPView.NuiWindow)
        {
            _defaultPortrait = subject.PortraitResRef+'h';
            _initialPortrait = _defaultPortrait;
            _subject = subject;

            _model = new(_placeableResRefs, PPView.ColumnCount);
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
        public PortraitPickerController(NwPlayer player, NwItem subject) : base(player, PPView.NuiWindow)
        {
            _defaultPortrait = subject.PortraitResRef+'h';
            _initialPortrait = _defaultPortrait;
            _subject = subject;

            _model = new(_placeableResRefs, PPView.ColumnCount);
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

        protected override void OnClick(string elementId)
        {
            if (elementId == nameof(PPView.OkButton))
            {
                var str = _model.SelectedPortrait ?? _defaultPortrait+'h';

                if(_subject is NwCreature creature) creature.PortraitResRef = str[..(str.Length-1)];
                else ((NwPlaceable)_subject).PortraitResRef = str[..(str.Length-1)];
            }

            Close();
        }

        protected override object? OnClose(){return null;}
    }
}