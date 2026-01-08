using System.IO;

namespace QuestSystem
{
    public sealed class Quest
    {
        public static string? Serialize(Quest quest) => QuestSerializer.Serialize(quest);
        public static Quest? Deserialize(string json) => QuestSerializer.Deserialize<Quest>(json);
        public static Quest? Deserialize(Stream stream) => QuestSerializer.Deserialize<Quest>(stream);

        public string Tag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}