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

        internal override WrapperBase Wrap() => throw new NotSupportedException("Unknown nodes can not be wrapped.");
    }
}
