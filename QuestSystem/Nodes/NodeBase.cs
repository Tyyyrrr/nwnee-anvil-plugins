using System.Text.Json.Serialization;
using QuestSystem.Wrappers;

namespace QuestSystem.Nodes
{
    [JsonPolymorphic(
        IgnoreUnrecognizedTypeDiscriminators =false,
        TypeDiscriminatorPropertyName ="$nodeType",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
    [JsonDerivedType(typeof(UnknownNode),"$unknown")]
    [JsonDerivedType(typeof(StageNode),"$stage")]
    [JsonDerivedType(typeof(RewardNode),"$reward")]
    [JsonDerivedType(typeof(VisibilityNode),"$visibility")]
    [JsonDerivedType(typeof(RandomizerNode),"$randomizer")]
    [JsonDerivedType(typeof(CooldownNode),"$cooldown")]
    // ...
    public abstract class NodeBase : IWrappable
    {
        public virtual int ID {get;set;}
        public virtual int NextID {get;set;}
        public virtual bool Rollback {get;set;}

        WrapperBase IWrappable.Wrap() => Wrap();
        internal abstract WrapperBase Wrap();
    }
}