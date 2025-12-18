//////////////////////////////////////////
//:: inc_mvtsys.nss
// Interface for MovementSystem plugin
// Horses and aberration travel equipment need LocalInt "MvtSysRank" specifying a tier of the mount/item to work with this system.
//////////////////////////////////////////

const string LOCVAR_PARAM = "MvtSysCSBridgeParameter";
const string LOCVAR_RESULT = "MvtSysCSBridgeResult";

const int PARAM_PRINT = 1;
const int PARAM_CRAWL = 2;
const int PARAM_HORSE = 3;
const int PARAM_MOUNTING = 4;
const int PARAM_DISMOUNTING = 5;
const int PARAM_SURF_MAT_CHANGE = 6;
//...
//...

void PrintSpeed(object oPC);
void PrintSpeed(object oPC)
{
    if(oPC == OBJECT_INVALID || !GetIsPC(oPC))
        return;

    SetLocalInt(oPC,LOCVAR_PARAM,PARAM_PRINT);

    ExecuteScript("mvtsys_csbridge", oPC);
}

void RefreshCrawl(object oPC);
void RefreshCrawl(object oPC)
{
    if(oPC == OBJECT_INVALID || !GetIsPC(oPC))
        return;

    SetLocalInt(oPC,LOCVAR_PARAM,PARAM_CRAWL);

    ExecuteScript("mvtsys_csbridge", oPC);
}

void OnMountingBegin(object oPC);
void OnMountingBegin(object oPC)
{
    if(oPC == OBJECT_INVALID || !GetIsPC(oPC))
        return;

    SetLocalInt(oPC,LOCVAR_PARAM,PARAM_MOUNTING);

    ExecuteScript("mvtsys_csbridge", oPC);
}

void OnDismountingBegin(object oPC);
void OnDismountingBegin(object oPC)
{
    if(oPC == OBJECT_INVALID || !GetIsPC(oPC))
        return;

    SetLocalInt(oPC,LOCVAR_PARAM,PARAM_DISMOUNTING);

    ExecuteScript("mvtsys_csbridge", oPC);
}

void OnSurfaceMaterialChanged(object oPC);
void OnSurfaceMaterialChanged(object oPC)
{
    if(oPC == OBJECT_INVALID || !GetIsPC(oPC))
        return;

    SetLocalInt(oPC,LOCVAR_PARAM, PARAM_SURF_MAT_CHANGE);

    ExecuteScript("mvtsys_csbridge", oPC);
}