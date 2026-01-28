using System.Collections.Generic;
using System.Linq;
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

        public override object Clone()
        {
            var objects = Objects.Select(i => new KeyValuePair<string, bool>((string)i.Key.Clone(), i.Value)).ToDictionary();

            return new VisibilityNode()
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback,
                
                Objects = objects
            };
        }
    }
}