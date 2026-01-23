using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuestSystem.Nodes
{
    internal class NodeConverter : JsonConverter<NodeBase>
    {
        private readonly JsonSerializerOptions _options;
        public NodeConverter(JsonSerializerOptions options)
        {
            _options = options;
        }

        public override NodeBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            NodeBase? node = null;

            if (root.TryGetProperty("$nodeType", out var typeProp))
            {
                var type = typeProp.GetString();
                node = type switch
                {
                    "$stage" => root.Deserialize<StageNode>(_options),
                    "$reward" => root.Deserialize<RewardNode>(_options),
                    "$visibility" => root.Deserialize<VisibilityNode>(_options),
                    "$randomizer" => root.Deserialize<RandomizerNode>(_options),
                    "$cooldown" => root.Deserialize<CooldownNode>(_options),
                    _ => new UnknownNode(doc.RootElement.GetRawText())
                };
            }

            return node ?? new UnknownNode(root.GetRawText());
        }

        public override void Write(Utf8JsonWriter writer, NodeBase value, JsonSerializerOptions _)
        {
            writer.WriteStartObject();

            writer.WriteString("$nodeType", value switch
            {
                StageNode => "$stage",
                RewardNode => "$reward",
                VisibilityNode => "$visibility",
                RandomizerNode => "$randomizer",
                CooldownNode => "$cooldown",
                _ => "$unknown"
            });

            var type = value.GetType();
            var json = JsonSerializer.SerializeToElement(value, type, _options);

            foreach (var prop in json.EnumerateObject())
                prop.WriteTo(writer);

            writer.WriteEndObject();
        }

    }
}