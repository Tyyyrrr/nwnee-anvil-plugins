using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using QuestSystem.Objectives;

namespace QuestSystem
{
    public static class QuestSerializer
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Replace,
            WriteIndented = false,
            AllowTrailingCommas = false,
            MaxDepth = 6,
            IncludeFields = false,
        };

        private static FrozenSet<Type> _validTypes = new HashSet<Type>()
        {
            typeof(Quest),
            typeof(QuestStage),
            typeof(Objective),
            typeof(QuestStageReward)
        }.ToFrozenSet();

        private static readonly FrozenSet<string> _emptyJsonValues = new HashSet<string>()
        {
            "{}",
            "[]",
            "null"
        }.ToFrozenSet();

        private static bool IsValidType(Type type) => _validTypes.Any(t => t.IsAssignableFrom(type));
        internal static string? Serialize<T>(T obj) where T : class
        {
            if (!IsValidType(typeof(T))) return null;
            var str = JsonSerializer.Serialize(obj, _jsonOptions);
            return (string.IsNullOrEmpty(str) || _emptyJsonValues.Contains(str)) ? null : str;
        }

        internal static T? Deserialize<T>(string json) where T : class
        {
            return IsValidType(typeof(T)) ? JsonSerializer.Deserialize<T>(json, _jsonOptions) : null;
        }

        internal static T? Deserialize<T>(Stream stream) where T : class
        {
            var t = IsValidType(typeof(T)) ? JsonSerializer.Deserialize<T>(stream, _jsonOptions) : null;
            stream.Close();
            return t;
        }
    }
}