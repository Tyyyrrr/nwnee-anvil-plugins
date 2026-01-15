using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QuestSystem.Nodes;
using QuestSystem.Objectives;

namespace QuestSystem
{
    public static class QuestSerializer
    {
        static QuestSerializer()
        {
            _validTypes = new HashSet<Type>()
            {
                typeof(Quest),
                typeof(NodeBase),
                typeof(Objective),
                typeof(QuestStageReward)
            }.ToFrozenSet();

            _emptyJsonValues = new HashSet<string>()
            {
                "{}",
                "[]",
                "null"
            }.ToFrozenSet();

            _jsonOptions = new JsonSerializerOptions()
            {
                PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Replace,
                WriteIndented = false,
                AllowTrailingCommas = false,
                MaxDepth = 6,
                IncludeFields = false,
            };

            _jsonOptions.Converters.Add(new NodeConverter());

        }
        private static readonly JsonSerializerOptions _jsonOptions;

        private static readonly FrozenSet<Type> _validTypes;

        private static readonly FrozenSet<string> _emptyJsonValues;

        public static bool IsValidType(Type type) => _validTypes.Any(t => t.IsAssignableFrom(type));
        public static string? Serialize<T>(T obj) where T : class
        {
            if (!IsValidType(typeof(T))) return null;
            var str = JsonSerializer.Serialize(obj, _jsonOptions);
            return (string.IsNullOrEmpty(str) || _emptyJsonValues.Contains(str)) ? null : str;
        }

        public static T? Deserialize<T>(string json) where T : class
        {
            return IsValidType(typeof(T)) ? JsonSerializer.Deserialize<T>(json, _jsonOptions) : null;
        }

        /// <summary>
        /// Deserializes an object from the provided stream.
        /// This method takes ownership of <paramref name="stream"/> and will close it.
        /// Callers should not dispose the stream.
        /// </summary>
        public static T? Deserialize<T>(Stream ownedStream) where T : class
        {
            try
            {
                return IsValidType(typeof(T))
                    ? JsonSerializer.Deserialize<T>(ownedStream, _jsonOptions)
                    : null;
            }
            finally
            {
                ownedStream.Dispose();
            }
        }

        /// <inheritdoc cref="Deserialize{T}(Stream)"/>
        public static async Task<T?> DeserializeAsync<T>(Stream ownedStream) where T : class
        {
            try
            {
                return IsValidType(typeof(T))
                    ? await JsonSerializer.DeserializeAsync<T>(ownedStream, _jsonOptions)
                    : null;
            }
            finally
            {
                ownedStream.Dispose();
            }
        }

    }
}