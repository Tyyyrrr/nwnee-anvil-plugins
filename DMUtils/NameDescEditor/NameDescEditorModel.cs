using Anvil.API;

namespace DMUtils.NameDescEditor
{
    internal sealed class NameDescEditorModel
    {
        public string Name {get;set;}
        public string Description{get;set;}
        private readonly NwObject _target;
        public NameDescEditorModel(NwObject target)
        {
            _target = target;
            Name=target.Name;
            Description = target.Description;
        }
        public void Apply()
        {
            _target.Name = Name;
            _target.Description = Description;
        }
    }
}