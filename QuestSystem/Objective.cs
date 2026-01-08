using System;
using System.IO;
using System.Text.Json.Serialization;
using QuestSystem.Wrappers;

namespace QuestSystem.Objectives
{
    [JsonPolymorphic(
        IgnoreUnrecognizedTypeDiscriminators = false,
        TypeDiscriminatorPropertyName = "$objectiveType",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(ObjectiveDeliver), "$deliver")]
    [JsonDerivedType(typeof(ObjectiveExplore), "$explore")]
    [JsonDerivedType(typeof(ObjectiveInteract), "$interact")]
    [JsonDerivedType(typeof(ObjectiveKill), "$kill")]
    [JsonDerivedType(typeof(ObjectiveObtain), "$obtain")]
    [JsonDerivedType(typeof(ObjectiveSpellcast), "$spellcast")]
    public abstract class Objective
    {
        public static string? Serialize(Objective questStageReward) => QuestSerializer.Serialize(questStageReward);
        public static Objective? Deserialize(string json) => QuestSerializer.Deserialize<Objective>(json);
        public static Objective? Deserialize(Stream stream) => QuestSerializer.Deserialize<Objective>(stream);

        public string JournalEntry { get; set; } = string.Empty;
        public int NextStageID { get; set; } = -1;
        public bool PartyMembersAllowed { get; set; } = false;
        public bool ShowInJournal { get; set; } = false;

        public string[] AreaTags { get; set; } = Array.Empty<string>();
        public QuestStageReward Reward { get; set; } = new();

        internal abstract ObjectiveWrapper Wrap();
        internal abstract IObjectiveProgress CreateProgressTrack();
    }
}