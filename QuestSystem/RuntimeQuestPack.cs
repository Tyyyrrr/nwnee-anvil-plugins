using System;
using System.IO;
using System.Linq;
using QuestSystem.Nodes;

namespace QuestSystem
{
    /// <summary>
    /// Provides synchronous read-only access to the file for gameplay purposes. 
    /// </summary>
    internal sealed class RuntimeQuestPack : QuestPack
    {
        public RuntimeQuestPack(Stream stream) : base(stream, readOnly: true){}

        internal Quest? GetQuest(string questTag)
        {
            var entry = _archive.GetEntry(GetQuestEntryPath(questTag));
            if (entry == null)
                return null;

            using var stream = entry.Open();
            return DefaultSerializer.DeserializeQuestFromStream(stream);
        }
        internal NodeBase? GetNode(string questTag, int id)
        {
            var entry = _archive.GetEntry(GetNodeEntryPath(questTag, id));
            if (entry == null)
                return null;

            using var stream = entry.Open();
            return DefaultSerializer.DeserializeNodeFromStream(stream);
        }

        internal NodeBase[] GetNodes(string questTag, params int[] ids)
        {
            string questPath = GetQuestEntryPath(questTag);
            var nodes = new NodeBase[ids.Length];
            int count = 0;
            foreach(var entry in _archive.Entries)
            {
                if(!entry.FullName.StartsWith(questPath) || !int.TryParse(entry.FullName[questPath.Length..], out var id) || !ids.Contains(id))
                    continue;

                using var stream = entry.Open();

                var node = DefaultSerializer.DeserializeNodeFromStream(stream);

                if(node == null) 
                    return Array.Empty<NodeBase>();

                nodes[count++] = node;
            }
            return count == ids.Length ? nodes : Array.Empty<NodeBase>();
        }
    }
}