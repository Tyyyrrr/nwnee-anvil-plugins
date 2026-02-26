using System;
using Anvil.API;
using Anvil.Services;
using Jorteck.Toolbox.Features.ToolWindows;

namespace ToolboxAdapter
{
    [ServiceBinding(typeof(ToolboxAdapterService))]
    public sealed class ToolboxAdapterService
    {
        private readonly Jorteck.Toolbox.Core.WindowManager _winMan;
        public ToolboxAdapterService(Jorteck.Toolbox.Core.WindowManager winMan)
        {
            _winMan = winMan;
        }

        private enum ToolboxWindows
        {
            Chooser = 1,
            CreatureBasic = 2,
            CreatureStats = 3,
            Transform = 4,
        }

        [ScriptHandler("toolbox_bridge")]
        public ScriptHandleResult HandleToolboxWindowRequest(CallInfo callInfo)
        {
            var pc = callInfo.ObjectSelf as NwCreature;

            if(pc == null || !pc.IsValid || !(pc.IsDMAvatar || pc.IsDMPossessed))
                return ScriptHandleResult.NotHandled;

            var player = pc.ControllingPlayer;

            if(player == null || !player.IsValid)
                return ScriptHandleResult.NotHandled;

            var locVar = pc.GetObjectVariable<LocalVariableInt>("DM_TBOXCMD");

            if(!locVar.HasValue)
                return ScriptHandleResult.NotHandled;

            int param = locVar.Value;

            locVar.Delete();

            if(param <= 0 || !Enum.IsDefined(typeof(ToolboxWindows),param))
                return ScriptHandleResult.NotHandled;

            switch ((ToolboxWindows)param)
            {
                case ToolboxWindows.Chooser:
                    _winMan.OpenWindow<ChooserWindowView,ChooserWindowController>(player);
                    break;

                case ToolboxWindows.CreatureBasic:
                    _winMan.OpenWindow<CreaturePropertiesBasicWindowView, CreaturePropertiesBasicWindowController>(player);
                    break;

                case ToolboxWindows.CreatureStats:
                    _winMan.OpenWindow<CreaturePropertiesStatsWindowView, CreaturePropertiesStatsWindowController>(player);
                    break;

                case ToolboxWindows.Transform:
                    _winMan.OpenWindow<VisualTransformWindowView,VisualTransformWindowController>(player);
                    break;

                default: return ScriptHandleResult.NotHandled;
            }

            return ScriptHandleResult.Handled;
        }
    }

}