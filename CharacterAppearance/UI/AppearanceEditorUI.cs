using System.Collections.Generic;
using Anvil.API;
using NuiMVC;

namespace CharacterAppearance.UI
{
    internal static class AppearanceEditorUI
    {
        private static readonly HashSet<ControllerBase> _instances = new();

        public static void Open(NwPlayer player, EditorFlags flags)
        {
            var pc = player.ControlledCreature;

            if (pc == null || !pc.IsValid) return;

            foreach(var i in _instances)
                if(((AppearanceEditorController)i).GetPlayer() == player)
                    return;

            var controller = new AppearanceEditorController(player, AppearanceEditorView.Window, flags);

            _instances.Add(controller);

            controller.ClosedEvent += Clear;
        }
        
        private static void Clear(ControllerBase cb, object? o)
        {
            cb.ClosedEvent -= Clear;

            var aec = (AppearanceEditorController)cb;

            var player = aec.GetPlayer();

            _ = _instances.Remove(cb);

            aec.Dispose();
            
            if(o is not bool b || !b) return;

            CharacterAppearanceService.RaiseOnBodyAppearanceEditComplete(player, b);
        }
    }
}