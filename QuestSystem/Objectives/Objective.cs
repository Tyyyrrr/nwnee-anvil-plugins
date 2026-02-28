using System;
using System.Text.Json.Serialization;
using QuestSystem.Wrappers.Objectives;
using ClockPlugin.Cooldowns;
using QuestSystem.Wrappers;

namespace QuestSystem.Objectives
{
    [JsonPolymorphic(
        IgnoreUnrecognizedTypeDiscriminators = false,
        TypeDiscriminatorPropertyName = "$objectiveType",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)] // todo: add safe fallback to unknown type with custom json converter
    [JsonDerivedType(typeof(ObjectiveDeliver), "$deliver")]
    [JsonDerivedType(typeof(ObjectiveExplore), "$explore")]
    [JsonDerivedType(typeof(ObjectiveInteract), "$interact")]
    [JsonDerivedType(typeof(ObjectiveKill), "$kill")]
    [JsonDerivedType(typeof(ObjectiveObtain), "$obtain")]
    [JsonDerivedType(typeof(ObjectiveSpellcast), "$spellcast")]
    public abstract class Objective : IWrappable, ICloneable
    {
        public string JournalEntry { get; set; } = string.Empty;
        public int NextStageID { get; set; } = -1;
        public bool PartyMembersAllowed { get; set; } = false;
        public bool ShowInJournal { get; set; } = false;
        
        public string[] AreaTags { get; set; } = Array.Empty<string>();
        public string[] TriggerTags { get; set; } = Array.Empty<string>();

        public ObjectiveTimer? Cooldown { get; set; } = null;

        public sealed class ObjectiveTimer : ICooldown, ICloneable
        {
            public string CooldownTag {get; set;} = string.Empty;
            public float DurationSeconds {get; set;} = 0;
            public bool RunOffline {get;set;} = true;

            public bool ShowInJournal{get; set;} = false;
            public JournalFormat Format {get; set;} = JournalFormat.CountDown;

            public enum JournalFormat
            {
                CountUp,
                CountDown
            }
            public object Clone()
            {
                return new ObjectiveTimer()
                {
                    CooldownTag = (string)this.CooldownTag.Clone(),
                    DurationSeconds = this.DurationSeconds,
                    RunOffline = this.RunOffline,
                    ShowInJournal = this.ShowInJournal,
                    Format = this.Format
                };
            }

        }


        WrapperBase IWrappable.Wrap() => Wrap();
        internal abstract WrapperBase Wrap();
        internal abstract IObjectiveProgress CreateProgressTrack();
        public abstract object Clone();
    }
}