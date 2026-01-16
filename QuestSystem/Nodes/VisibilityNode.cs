using System.Collections.Generic;
using QuestSystem.Wrappers;

namespace QuestSystem.Nodes
{
    public sealed class VisibilityNode : NodeBase
    {
        public Dictionary<string,bool> Objects {get;set;} = new();

        internal override WrapperBase Wrap()
        {
            throw new System.NotImplementedException();
        }
    }
}