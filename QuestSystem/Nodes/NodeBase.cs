using QuestSystem.Wrappers;

namespace QuestSystem.Nodes
{
    public abstract class NodeBase : IWrappable
    {
        public virtual int ID {get;set;} = -1;
        public virtual int NextID {get;set;} = -1;
        public virtual bool Rollback {get; set;} = false;

        WrapperBase IWrappable.Wrap() => Wrap();
        internal abstract WrapperBase Wrap();
    }
}