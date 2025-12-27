using System.Text.Json.Serialization;

namespace QuestSystem.Objectives.Templates
{
    [JsonPolymorphic(
        IgnoreUnrecognizedTypeDiscriminators = false, 
        TypeDiscriminatorPropertyName = "$objectiveType", 
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(ObjectiveTemplateDeliver),"$deliver")]
    [JsonDerivedType(typeof(ObjectiveTemplateExplore),"$explore")]
    [JsonDerivedType(typeof(ObjectiveTemplateInteract),"$interact")]
    [JsonDerivedType(typeof(ObjectiveTemplateKill),"$kill")]
    [JsonDerivedType(typeof(ObjectiveTemplateObtain),"$obtain")]
    public abstract class ObjectiveTemplate
    {
        
    }
}