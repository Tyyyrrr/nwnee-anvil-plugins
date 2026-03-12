using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using DMUtils.NameDescEditor;
using DMUtils.PortraitPicker;
using DMUtils.VFXSpawner;

namespace DMUtils
{
    [ServiceBinding(typeof(DMUtilsService))]
    public sealed class DMUtilsService
    {
        public DMUtilsService()
        {
            NwModule.Instance.OnModuleLoad += _ => PortraitPickerController.LoadPortraits();
        }


        static void OpenPortraitPicker(NwCreature owner, NwObject subject)
        {
            if(subject is NwPlaceable placeable)
                _ = new PortraitPickerController(owner.ControllingPlayer!, placeable);
            else if(subject is NwCreature creature)
                _ = new PortraitPickerController(owner.ControllingPlayer!, creature);
            else
            {
                owner.ControllingPlayer!.SendServerMessage("Narzędzia można użyć tylko na stworzeniu lub ustawialnym obiekcie.".ColorString(ColorConstants.Red));
            }
        }

        void OpenVFXSpawner(NwCreature owner, NwObject subject)
        {
            _ = new VFXSpawnerController(owner.ControllingPlayer!, subject);
        }
        void OpenVFXSpawner(NwCreature owner, Vector3 position)
        {
            _ = new VFXSpawnerController(owner.ControllingPlayer!, Location.Create(owner.Area!,position, owner.Rotation)!);
        }

        void OpenNameDescEditor(NwCreature owner, NwObject target)
        {
            _ = new NameDescEditorController(owner.ControllingPlayer!, target);
        }

        [ScriptHandler("mod_evt_target")]
        public ScriptHandleResult HandleTargetEvent(CallInfo info)
        {
            if(info.TryGetEvent<ModuleEvents.OnPlayerTarget>(out var eventData))
            {
                var selector = eventData.Player.ControlledCreature;

                if(selector == null || eventData.IsCancelled)
                    return ScriptHandleResult.NotHandled;
                
                var cmd = selector.GetObjectVariable<LocalVariableString>("DM_COMMANDS");
                var par = selector.GetObjectVariable<LocalVariableString>("DM_COMMANDS_PARAM");

                if(!string.IsNullOrEmpty(par.Value))
                    return ScriptHandleResult.NotHandled;

                var val = cmd.Value;

                if(string.IsNullOrEmpty(val))
                    return ScriptHandleResult.NotHandled;

                
                if(val == "portrait")
                {
                    if(eventData.TargetObject is NwCreature creature) OpenPortraitPicker(selector,creature);
                    else if(eventData.TargetObject is NwPlaceable placeable) OpenPortraitPicker(selector, placeable);
                    else if(eventData.TargetObject is NwItem item) OpenPortraitPicker(selector,item);
                    else return ScriptHandleResult.NotHandled;
                }
                else if(val == "vfx")
                {
                    if(eventData.TargetObject is NwCreature || eventData.TargetObject is NwPlaceable)
                        OpenVFXSpawner(selector,eventData.TargetObject);
                    else OpenVFXSpawner(selector, eventData.TargetPosition);
                }
                else if(val == "description" && eventData.TargetObject != null)
                {
                    OpenNameDescEditor(selector,eventData.TargetObject);
                }
                else return ScriptHandleResult.NotHandled;
            
                cmd.Delete();
                par.Delete();

                return ScriptHandleResult.Handled;
            }

            return ScriptHandleResult.NotHandled;
        }
    }
}