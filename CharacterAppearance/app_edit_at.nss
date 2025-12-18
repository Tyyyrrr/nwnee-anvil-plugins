//////////////////////////
// :: app_edit_at.nss
// Use this in conversations to open AppearanceEditor NUI.
// Set script param with name of const variable (see below) to "1" to enable that feature in editor
// 

const int BODY_TATTOO = 4;
const int BODY_HEAD = 10;
const int ARMOR_LIGHT = 32;
const int ARMOR_MEDIUM = 64;
const int ARMOR_HEAVY = 128;
const int WEAPON_MELEE = 256;
const int WEAPON_RANGED = 512;
const int WEAPON_MAGIC = 1024;


void main()
{
    object oPC = GetPCSpeaker();
    if(!GetIsPC(oPC)) return;

    int nFlags = 0;

    string sFlags = GetScriptParam("BODY_TATTOO");
    if(sFlags == "1"){
        nFlags |= BODY_TATTOO;
    }

    sFlags = GetScriptParam("BODY_HEAD");
    if(sFlags == "1"){
        nFlags |= BODY_HEAD;
    }

    sFlags = GetScriptParam("ARMOR_LIGHT");
    if(sFlags == "1"){
        nFlags |= ARMOR_LIGHT;
    }

    sFlags = GetScriptParam("ARMOR_MEDIUM");
    if(sFlags == "1"){
        nFlags |= ARMOR_MEDIUM;
    }

    sFlags = GetScriptParam("ARMOR_HEAVY");
    if(sFlags == "1"){
        nFlags |= ARMOR_HEAVY;
    }

    sFlags = GetScriptParam("WEAPON_MELEE");
    if(sFlags == "1"){
        nFlags |= WEAPON_MELEE;
    }

    sFlags = GetScriptParam("WEAPON_RANGED");
    if(sFlags == "1"){
        nFlags |= WEAPON_RANGED;
    }

    sFlags = GetScriptParam("WEAPON_MAGIC");
    if(sFlags == "1"){
        nFlags |= WEAPON_MAGIC;
    }

    if(nFlags == 0) return;

    sFlags = IntToString(nFlags);

    SetScriptParam("AppearanceEditorFlags", sFlags);
    ExecuteScript("app_edit_nui", oPC);
}