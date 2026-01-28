using System;
using QuestSystem.Wrappers;

namespace QuestSystem.Nodes
{    
    public class UnknownNode : NodeBase
    {
        public override int ID {get => -1;}
        public override int NextID {get => -1;}
        public override bool Rollback { get => false; }
        public string RawData {get;}
        public UnknownNode(string raw){ RawData = raw; }
        public UnknownNode() : this(string.Empty) {}

        internal override WrapperBase Wrap() => throw new UnknownNodeWrapException(this);

        public override object Clone()
        {
            return new UnknownNode((string)this.RawData.Clone())
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback
            };
        }
    }
    internal sealed class UnknownNodeWrapException : Exception
    {
        public override string Message => "Unknown nodes can not be wrapped.";
        public string RawNodeData {get;}
        public UnknownNodeWrapException(UnknownNode node)
        {
            RawNodeData = node.RawData;
        }
    }
}
