namespace QuestSystem.Nodes
{    
    public class UnknownNode : NodeBase
    {
        public override int ID => -1;
        public override int NextID => -1;
        public string RawData {get;}
        public UnknownNode(string raw){ RawData = raw; }
        public UnknownNode() : this(string.Empty) {}
    }
}
