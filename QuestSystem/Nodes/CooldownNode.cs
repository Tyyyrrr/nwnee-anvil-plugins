namespace QuestSystem.Nodes
{
    public class CooldownNode : NodeBase
    {
        public string CooldownTag {get;set;} = string.Empty;
        public float DurationSeconds {get;set;}
        public bool RunOffline {get;set;}
    }
}