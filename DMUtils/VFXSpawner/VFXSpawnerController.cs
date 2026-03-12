using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using NuiMVC;

using VFXSModel = DMUtils.VFXSpawner.VFXSpawnerModel;
using VFXSView = DMUtils.VFXSpawner.VFXSpawnerView;


namespace DMUtils.VFXSpawner
{
    internal sealed class VFXSpawnerController : ControllerBase
    {
        private readonly VFXSModel _model;

        public VFXSpawnerController(NwPlayer player, NwObject targetObject) : base(player, VFXSView.NuiWindow)
        {
            _model = new(targetObject,player);
            InitializeBindValues();
        }
        
        public VFXSpawnerController(NwPlayer player, Location targetLocation) : base(player, VFXSView.NuiWindow)
        {
            _model = new(targetLocation,player);
            InitializeBindValues();
        }

        private List<NuiComboEntry> tempEffects = new();
        void InitializeBindValues()
        {
            tempEffects = VFXSModel.GetTemporaryVFXes().Select(kvp=>new NuiComboEntry(kvp.Key,kvp.Value)).ToList();
            SetValue(VFXSView.ComboEntriesProperty, tempEffects);
            SetValue(VFXSView.DurationTextEditValueProperty,"-1");

            SetWatch(VFXSView.SelectedVFXEntryProperty,true);
            SetWatch(VFXSView.DurationTextEditValueProperty,true);
        }

        protected override void Update(string elementId)
        {
            if(elementId == nameof(VFXSView.SelectedVFXEntryProperty))
            {                
                var entryId = GetValue(VFXSView.SelectedVFXEntryProperty);
                if(entryId >= 0 && entryId < tempEffects.Count)
                {
                    var entryLabel = tempEffects[entryId];
                    SetValue(VFXSView.SelectedVFXIndexStringProperty,entryLabel.Value.ToString());
                    _model.SelectedVFXID = entryLabel.Value;
                }
            }
            else if(elementId == nameof(VFXSView.DurationTextEditValueProperty))
            {
                var val = GetValue(VFXSView.DurationTextEditValueProperty);
                if(!int.TryParse(val, out var num))
                {
                    SetWatch(VFXSView.DurationTextEditValueProperty,false);
                    SetValue(VFXSView.DurationTextEditValueProperty,_model.DurationSeconds.ToString());
                    SetWatch(VFXSView.DurationTextEditValueProperty,true);
                }
                else
                {
                    _model.DurationSeconds = num;
                }
            }
        }
        protected override void OnClick(string elementId)
        {
            if (elementId == nameof(VFXSView.OkButton))
                _model.SpawnVFX();

            else Close();
        }

        protected override object? OnClose(){return null;}
    }
}