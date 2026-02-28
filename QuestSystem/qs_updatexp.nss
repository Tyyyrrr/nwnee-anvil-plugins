////////////////////////////
// :: qs_updatexp.nss
// This script is called from C# 'QuestSystem' anvil plugin when XP reward is given to the player.
// Use it to perform any additional XP updates/notifications for this PC
/////////////////////////////

#include "inc_sql_player"

void main()
{
    object oPC = OBJECT_SELF;

    if(!GetIsObjectValid(oPC))
        return;

    string sXP = GetScriptParam("XP");
    if(sXP == "")
        return;

    int nXP = StringToInt(sXP);
    if(nXP <= 0)
        return;

    sql_UpdatePlayerXP(oPC,nXP,"Quest");
}
