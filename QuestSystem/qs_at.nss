//////////////////////////
//:: qs_at.nss
//
// Script parameters:           Format:                 (whitespaces are ignored)
// - "GiveQuest"                "questA:2; questB:10"       - set player on selected quest stage and update journal
// - "CompleteQuest"            "questA:2; questB:10"       - complete quest on selected stage, reward won't be granted
// - "ClearQuest"               "questA; questB"            - remove all quest data associated with the player
// - "CompleteStage"            "questA:2,4,6; questB:10"   - grant reward for completing the stage and auto advance to the next stage (if any)
//                                                                                              ^ works only for stages with no objectives!
//////////////////////////

void main(){
    WriteTimestampedLogEntry("C# not handled \'qs_at.nss\', fallback to default NWScript");
}