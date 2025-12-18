////////////////////////
//: mvtsys_csbridge.nss
////////////////////////
#include "inc_mvtsys"
#include "x3_inc_horse"


void main()
{
    object oPC = OBJECT_SELF;

    if(oPC == OBJECT_INVALID || !GetIsPC(oPC))
    {
        WriteTimestampedLogEntry("OBJECT_SELF is invalid or not PC");
        DeleteLocalInt(oPC, LOCVAR_PARAM);
        return;
    }

    int nParam = GetLocalInt(oPC,LOCVAR_PARAM);

    if(nParam <= 0)
    {
        if(GetScriptParam(LOCVAR_PARAM) == "horse")
        {
            string sHorseResRef = HORSE_SupportMountResRef(oPC);
            if (sHorseResRef == ""){
                DeleteLocalInt(oPC,LOCVAR_RESULT);
                return;
            }
            SetLocalString(oPC, LOCVAR_RESULT, sHorseResRef);
            return;
        }

        WriteTimestampedLogEntry("Invalid parameter");
        DeleteLocalInt(oPC, LOCVAR_PARAM);
        return;
    }

    switch(nParam)
    {
        case PARAM_HORSE:
        {
            string sHorseResRef = HORSE_SupportMountResRef(oPC);
            if (sHorseResRef == ""){
                DeleteLocalInt(oPC,LOCVAR_RESULT);
                return;
            }
            SetLocalString(oPC, LOCVAR_RESULT, sHorseResRef);
            break;
        }

        /////////////////////////////////////
        // comment out in release
        case PARAM_PRINT:
        {
            WriteTimestampedLogEntry("C# not handled script (@speed)" );
            break;
        }

        case PARAM_CRAWL:
        {
            WriteTimestampedLogEntry("C# not handled script (@crawl)" );
            break;
        }

        case PARAM_MOUNTING:
        {
            WriteTimestampedLogEntry("C# not handled script (mounting)" );
            break;
        }

        case PARAM_DISMOUNTING:
        {
            WriteTimestampedLogEntry("C# not handled script (dismounting)" );
            break;
        }
        case PARAM_SURF_MAT_CHANGE:
        {
            WriteTimestampedLogEntry("C# not handled script (surface material change)" );
            break;
        }
        ///////////////////////////////////////
    }

    DeleteLocalInt(oPC, LOCVAR_PARAM);
}