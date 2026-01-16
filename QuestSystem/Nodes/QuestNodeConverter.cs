using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuestSystem.Nodes
{
    public class NodeConverter : JsonConverter<NodeBase>
    {
        public override NodeBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var raw = root.GetRawText();

            NodeBase? node = null;

            if (root.TryGetProperty("$nodeType", out var typeProp))
            {
                var type = typeProp.GetString();
                node = type switch
                {
                    "$stage" => JsonSerializer.Deserialize<StageNode>(raw, options),
                    "$reward" => JsonSerializer.Deserialize<RewardNode>(raw, options),
                    "$visibility" => JsonSerializer.Deserialize<VisibilityNode>(raw, options),
                    "$randomizer" => JsonSerializer.Deserialize<RandomizerNode>(raw, options),
                    "$cooldown" => JsonSerializer.Deserialize<CooldownNode>(raw, options),
                    //...
                    _ => new UnknownNode(raw)
                };

            }

            return node ?? new UnknownNode(raw);
        }

        public override void Write(Utf8JsonWriter writer, NodeBase value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, (object)value, options);
    }
}