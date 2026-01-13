using System.Text.Json.Serialization;

namespace QuestSystem.Nodes
{
    [JsonPolymorphic(
        IgnoreUnrecognizedTypeDiscriminators =false,
        TypeDiscriminatorPropertyName ="$nodeType",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
    [JsonDerivedType(typeof(QuestStageNode),"$stage")]
    [JsonDerivedType(typeof(RandomizerNode),"$randomizer")]
    [JsonDerivedType(typeof(CooldownNode),"$cooldown")]
    [JsonDerivedType(typeof(UnknownNode),"$unknown")]

    public abstract class NodeBase
    {
        public virtual int ID {get;}
        public virtual int NextID {get;}
    }
}