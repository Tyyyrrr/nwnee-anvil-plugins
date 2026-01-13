using System.Collections.Generic;

namespace QuestSystem.Nodes
{
    public sealed class VisibilityNode
    {
        public Dictionary<string,bool> Objects {get;set;} = new();
    }
}