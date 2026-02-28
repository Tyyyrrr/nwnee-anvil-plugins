using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

using QuestSystem.Nodes;

namespace QuestSystem
{
    /// <summary>
    /// Provides synchronous read/write access to the file.
    /// </summary>
    public sealed class EditorQuestPack : QuestPack
    {
        private readonly IQuestDataSerializer _serializer;

        public EditorQuestPack(Stream stream, bool readOnly, IQuestDataSerializer serializer) : base(stream, readOnly)
        {
            _serializer = serializer;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <param name="ownedStream">The archive. Callers should not dispose it manually.</param>
        /// <param name="serializer">Serialization interface, fallback to built-in default if null.</param>
        /// <exception cref="ArgumentException"></exception>
        public static EditorQuestPack OpenRead(Stream ownedStream, IQuestDataSerializer? serializer = null)
        {
            if(!ownedStream.CanRead) 
                throw new ArgumentException("Provided stream is not readable");

            return new EditorQuestPack(ownedStream, true, serializer ?? DefaultSerializer);
        }

        /// <inheritdoc cref="OpenRead"/>
        public static EditorQuestPack OpenWrite(Stream ownedStream, IQuestDataSerializer? serializer = null)
        {
            if(!ownedStream.CanWrite) 
                throw new ArgumentException("Provided stream is not writeable");

            return new EditorQuestPack(ownedStream, false, serializer ?? DefaultSerializer);
        }



        /// <returns>False if there is a quest with the same tag in the pack already, or an exception occurred.</returns>
        public bool WriteQuest(Quest quest)
        {
            ThrowIfReadOnly();

            string questPath = GetQuestEntryPath(quest.Tag);
            try
            {
                if (_archive.Entries.Any(e => e.FullName == questPath))
                    return false;

                var entry = _archive.CreateEntry(questPath, CompressionLevel.NoCompression);

                using var stream = entry.Open();

                _serializer.SerializeQuestToStream(quest, stream);

                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return false; }
        }

        /// <returns>False if there is no such quest, or the node with the same ID already exists in the pack, or an exception occurred.</returns>
        public bool WriteNode(Quest quest, NodeBase node)
        {
            ThrowIfReadOnly();

            string questPath = GetQuestEntryPath(quest.Tag);
            string nodePath = GetNodeEntryPath(quest.Tag, node.ID);
            try
            {
                bool noQuest = true;
                foreach (var e in _archive.Entries)
                {
                    if (e.FullName == questPath)
                        noQuest = false;
                    else if (e.FullName == nodePath)
                        return false;
                }
                if (noQuest) return false;

                var entry = _archive.CreateEntry(nodePath, CompressionLevel.NoCompression);

                using var stream = entry.Open();

                _serializer.SerializeNodeToStream(node, stream);

                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return false; }
        }

        /// <param name="serializedMetadata">Arbitrary object, serialized to JSON</param>
        /// <returns>False if there is no such quest, or there is some metadata for this quest alerady in the pack, or an exception occurred.</returns>
        public bool WriteMetadata(string questTag, string serializedMetadata)
        {
            ThrowIfReadOnly();

            string questPath = GetQuestEntryPath(questTag);
            string metadataPath = GetMetadataEntryPath(questTag);
            try
            {
                bool noQuest = true;
                foreach (var e in _archive.Entries)
                {
                    if (e.FullName == questPath)
                        noQuest = false;
                    else if (e.FullName == metadataPath)
                        return false;
                }
                if (noQuest) return false;

                var entry = _archive.CreateEntry(metadataPath, CompressionLevel.SmallestSize);

                using var sw = new StreamWriter(entry.Open(), leaveOpen: false);

                sw.Write(serializedMetadata);

                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return false; }
        }

        /// <returns>False if the quest does not exist, or an exception occurred.</returns>
        public bool RemoveQuest(string questTag)
        {
            ThrowIfReadOnly();

            var questPath = GetQuestEntryPath(questTag);
            var metadataPath = GetMetadataEntryPath(questTag);

            try
            {
                var questEntry = _archive.GetEntry(questPath);

                if (questEntry == null) return false;

                var entriesToDelete = new List<ZipArchiveEntry>();

                foreach (var e in _archive.Entries)
                {
                    if (e.FullName.StartsWith(questPath)
                    && e.FullName.Length != questPath.Length
                    && (e.FullName == metadataPath || int.TryParse(e.FullName[questPath.Length..], out _)))
                        entriesToDelete.Add(e);
                }

                foreach (var questData in entriesToDelete)
                    questData.Delete();

                _archive.GetEntry(metadataPath)?.Delete();

                questEntry.Delete();

                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return false; }
        }

        /// <returns>False if the quest does not exist, or it does not contain node with specified ID, or an exception occurred.</returns>
        public bool RemoveNode(string questTag, int nodeID)
        {
            ThrowIfReadOnly();

            var questPath = GetQuestEntryPath(questTag);

            try
            {
                if (!_archive.Entries.Any(e => e.FullName == questPath))
                    return false;

                var entry = _archive.GetEntry(GetNodeEntryPath(questTag, nodeID));

                if (entry == null) return false;

                entry.Delete();

                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return false; }
        }

        /// <returns>False if the quest does not exist, or it does not carry metadata, or an exception occurred.</returns>
        public bool RemoveMetadata(string questTag)
        {
            ThrowIfReadOnly();

            var questPath = GetQuestEntryPath(questTag);

            try
            {
                if (!_archive.Entries.Any(e => e.FullName == questPath))
                    return false;

                var entry = _archive.GetEntry(GetMetadataEntryPath(questTag));

                if (entry == null) return false;

                entry.Delete();

                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return false; }

        }

        /// <returns>Null if the quest does not exist in the pack, or deserialization failed, or an exception occurred</returns>
        public Quest? GetQuest(string questTag)
        {
            try
            {  
                var entry = _archive.GetEntry(GetQuestEntryPath(questTag));

                if(entry == null) return default;

                using var stream = entry.Open();

                var obj = _serializer.DeserializeQuestFromStream(stream);

                return obj;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return null; }
        }


        /// <returns>Array of quests deserialized from the file (only the quests, without nodes), or null if any quest is corrupted, or an exception occurred</returns>
        public Quest[]? GetQuests()
        {
            try
            {
                var quests = new List<Quest>();

                foreach (var entry in _archive.Entries.Where(e => e.FullName.EndsWith('/')))
                {

                    using var stream = entry.Open();

                    var quest = _serializer.DeserializeQuestFromStream(stream);

                    if (quest == null) return null;

                    quests.Add(quest);
                }

                return quests.ToArray();
            }
            catch (Exception ex) { Trace.WriteLine(ex); return null; }
        }

        /// <returns>Array of nodes deserialized from the file, or null if quest does not exist in the pack, or an exception occurred.</returns>
        public NodeBase[]? GetNodes(string questTag)
        {
            var nodes = new List<NodeBase>();

            string questPath = GetQuestEntryPath(questTag);
            string metadataEntryPath = GetMetadataEntryPath(questTag);

            try
            {
                foreach (var entry in _archive.Entries)
                {
                    if (!entry.FullName.StartsWith(questPath)
                    || entry.FullName == questPath
                    || entry.FullName == metadataEntryPath
                    || !int.TryParse(entry.FullName[questPath.Length..],out _))
                        continue;

                    using var stream = entry.Open();

                    var node = _serializer.DeserializeNodeFromStream(stream);

                    if(node == null) return null;

                    nodes.Add(node);
                }

                return nodes.ToArray();
            }
            catch (Exception ex) { Trace.WriteLine(ex); return null; }
        }

        /// <returns>A single node deserialized from the file, or null if quest does not exist in the pack, or it does not contain node with this ID, or an exception occurred.</returns>
        public NodeBase? GetNode(string questTag, int nodeID)
        {
            string nodeEntryPath = GetNodeEntryPath(questTag, nodeID);

            try
            {
                var entry = _archive.GetEntry(nodeEntryPath);
                if(entry == null) return null;
                using var stream = entry.Open();

                return _serializer.DeserializeNodeFromStream(stream);
            }
            catch (Exception ex) { Trace.WriteLine(ex); return null; }
        }

        /// <param name="options">Explicit serializer options</param>
        /// <returns>Object deserialized from the file, or null if either quest does not exist, or it does not carry any metadata, or an exception occurred.</returns>
        public T? GetMetadata<T>(string questTag, JsonSerializerOptions? options = null)
        {
            try
            {  
                var entry = _archive.GetEntry(GetMetadataEntryPath(questTag));

                if(entry == null) return default;

                using var stream = entry.Open();

                var obj = JsonSerializer.Deserialize<T>(stream, options);

                return obj;
            }
            catch (Exception ex) { Trace.WriteLine(ex); return default; }
        }

    }
}