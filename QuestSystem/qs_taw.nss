//////////////////////////
//:: qs_taw.nss
//
// Script parameters:       Format:    
// - "IsOnQuest"            "questA:2,3,4; questB; questC:1"    (whitespaces are ignored, stages are optional)
// - "IsNotOnQuest"
// - "CompletedQuest"       
// - "NotCompletedQuest"    
// - "HasItemTag"           "itemATag:10; itemBTag"        (amount is optional. Default is 1)
// - "HasItemResRef"        "itemAResRef:10; itemBResRef"
// - "HasItemTagResRef"      "itemATag:itemAResRef:10; itemBTag:itemBResRef"
//////////////////////////

void main(){
    WriteTimestampedLogEntry("C# not handled \'qs_at.nss\', fallback to default NWScript");
}