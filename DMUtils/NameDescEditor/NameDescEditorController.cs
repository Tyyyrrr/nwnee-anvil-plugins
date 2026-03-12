using Anvil.API;
using NuiMVC;

using NDEModel = DMUtils.NameDescEditor.NameDescEditorModel;
using NDEView = DMUtils.NameDescEditor.NameDescEditorView;


namespace DMUtils.NameDescEditor
{
    internal sealed class NameDescEditorController : ControllerBase
    {
        private readonly NDEModel _model;

        public NameDescEditorController(NwPlayer player, NwObject targetObject) : base(player, NDEView.NuiWindow)
        {
            _model = new(targetObject);
            InitializeBindValues();
        }

        void InitializeBindValues()
        {
            SetValue(NDEView.NameTextEditValueProperty, _model.Name);
            SetValue(NDEView.DescriptionTextEditValueProperty,_model.Description);

            SetWatch(NDEView.NameTextEditValueProperty,true);
            SetWatch(NDEView.DescriptionTextEditValueProperty,true);
        }

        protected override void Update(string elementId)
        {
            if(elementId == nameof(NDEView.NameTextEditValueProperty))
            {                
                _model.Name = GetValue(NDEView.NameTextEditValueProperty);
            }
            else if(elementId == nameof(NDEView.DescriptionTextEditValueProperty))
            {
                _model.Description = GetValue(NDEView.DescriptionTextEditValueProperty);
            }
        }
        protected override void OnClick(string elementId)
        {
            if (elementId == nameof(NDEView.OkButton))
                _model.Apply();
            Close();
        }

        protected override object? OnClose(){return null;}
    }
}