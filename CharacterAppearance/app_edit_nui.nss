/////////////////////////////////////////////////////////////////////////////////////////////////////////
//:: app_edit_nui.nss
//
// Attatch this handler to placeable's OnUsed event to make CharacterAppearance editor NUI pop-up for the interacting PC.
//    OBJECT_SELF needs a LocalInt "AppearanceEditorFlags" with a valid flag to work with the system.
//    If the local variable is present, system will treat oSelf as a Placeable, and obtain PC with GetLastUsedBy()
//
// Alternatively, the script can carry a ScriptParam "AppearanceEditorFlags" with the flag value as string.
//    This approach would work with ExecuteScript()
//
//
//     All available flags can be found in server log right after the module is loaded.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////
void main()
{
    WriteTimestampedLogEntry("C# CharacterAppearance plugin did not handle NUI pop-up request. Fallback to NWScript");
}