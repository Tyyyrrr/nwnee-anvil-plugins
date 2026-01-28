using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Nodes
{
    public class CooldownNode : NodeBase
    {
        public string CooldownTag {get;set;} = string.Empty;
        public float DurationSeconds {get;set;}
        public bool RunOffline {get;set;}
        public override object Clone()
        {
            return new CooldownNode()
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback,
                CooldownTag = (string)this.CooldownTag.Clone(),
                DurationSeconds = this.DurationSeconds,
                RunOffline = this.RunOffline,
            };
        }
        internal override CooldownNodeWrapper Wrap() => new(this);
    }
}