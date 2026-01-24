using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuestSystem.Nodes;

namespace QuestSystem
{
    
    /// <summary>
    /// Provides asynchronous read/write access to the file for GUI editor apps. 
    /// </summary>
    public sealed class EditorQuestPack : QuestPack
    {
        private readonly SemaphoreSlim _zipLock;
        private readonly CancellationTokenSource _packCTS;        
        private readonly CancellationTokenSource _linkedCTS;
        private readonly CancellationToken _linkedCT;

        private readonly IQuestDataSerializer _serializer;

        public EditorQuestPack(Stream stream, bool readOnly, IQuestDataSerializer serializer, CancellationToken globalToken = default) : base(stream, readOnly)
        {
            _zipLock = new(1,1);
            _packCTS = new();
            _linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(_packCTS.Token, globalToken);
            _linkedCT = _linkedCTS.Token;
            _serializer = serializer;
        }

        protected override void Dispose(bool disposing)
        {
            _packCTS.Cancel();

            _zipLock.Wait();

            _packCTS.Dispose();
            _linkedCTS.Dispose();

            _zipLock.Dispose();

            base.Dispose(disposing);
        }

        /// <param name="ownedStream">The archive. Callers should not dispose it manually.</param>
        /// <param name="serializer">Serialization interface, fallback to built-in default if null.</param>
        /// <param name="globalToken">Mechanism for cancelling ongoing I/O and/or serialization operations.</param>
        /// <exception cref="ArgumentException"></exception>
        public static EditorQuestPack OpenRead(Stream ownedStream, IQuestDataSerializer? serializer = null, CancellationToken globalToken = default)
        {
            if(!ownedStream.CanRead) 
                throw new ArgumentException("Provided stream is not readable");

            return new EditorQuestPack(ownedStream, true, serializer ?? DefaultSerializer, globalToken);
        }

        /// <inheritdoc cref="OpenRead"/>
        public static EditorQuestPack OpenWrite(Stream ownedStream, IQuestDataSerializer? serializer = null, CancellationToken globalToken = default)
        {
            if(!ownedStream.CanWrite) 
                throw new ArgumentException("Provided stream is not writeable");

            return new EditorQuestPack(ownedStream, false, serializer ?? DefaultSerializer, globalToken);
        }

        /// <returns>False if there is a quest with the same tag in the pack already.</returns>
        public async Task<bool> WriteQuestAsync(Quest quest)
        {
            ThrowIfReadOnly();

            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                _linkedCT.ThrowIfCancellationRequested();

                string questPath = GetQuestEntryPath(quest.Tag);

                if(_archive.Entries.Any(e=>e.FullName == questPath))
                    return false;

                _linkedCT.ThrowIfCancellationRequested();

                var entry = _archive.CreateEntry(questPath);

                using var stream = entry.Open();

                await _serializer.SerializeToStreamAsync(quest, stream); // no cancellation here, to prevent zip corruption

                return true;
            }
            finally
            {
                _zipLock.Release();
            }
        }

        /// <returns>False if there is no such quest, or the node with the same ID already exists in the pack.</returns>
        public async Task<bool> WriteNodeAsync(Quest quest, NodeBase node)
        {
            ThrowIfReadOnly();

            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                _linkedCT.ThrowIfCancellationRequested();

                string questPath = GetQuestEntryPath(quest.Tag);
                string nodePath = GetNodeEntryPath(quest.Tag, node.ID);
                bool noQuest = true;
                foreach(var e in _archive.Entries)
                {
                    _linkedCT.ThrowIfCancellationRequested();
                    if(e.FullName == questPath)
                        noQuest = false;
                    else if(e.FullName == nodePath)
                        return false;
                }
                if(noQuest) return false;

                var entry = _archive.CreateEntry(nodePath);

                using var stream = entry.Open();

                await _serializer.SerializeToStreamAsync(node, stream); // no cancellation here, to prevent zip corruption

                return true;
            }
            finally
            {
                _zipLock.Release();
            }
        }

        /// <param name="serializedMetadata">Arbitrary object, serialized to JSON</param>
        /// <returns>False if there is no such quest, or there is some metadata for this quest alerady in the pack.</returns>
        public async Task<bool> WriteMetadataAsync(string questTag, string serializedMetadata)
        {
            ThrowIfReadOnly();

            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                _linkedCT.ThrowIfCancellationRequested();

                string questPath = GetQuestEntryPath(questTag);
                string metadataPath = GetMetadataEntryPath(questTag);
                bool noQuest = true;
                foreach(var e in _archive.Entries)
                {
                    _linkedCT.ThrowIfCancellationRequested();
                    if(e.FullName == questPath)
                        noQuest = false;
                    else if(e.FullName == metadataPath)
                        return false;
                }
                if(noQuest) return false;

                var entry = _archive.CreateEntry(metadataPath);
                
                using var sw = new StreamWriter(entry.Open(), leaveOpen: false);

                await sw.WriteAsync(serializedMetadata); // no cancellation here, to prevent zip corruption

                return true;
            }
            finally
            {
                _zipLock.Release();
            }
        }

        /// <returns>False if the quest does not exist.</returns>
        public async Task<bool> RemoveQuestAsync(string questTag)
        {
            ThrowIfReadOnly();

            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                return await Task.Run(()=>
                {
                    var questPath = GetQuestEntryPath(questTag);
                    var metadataPath = GetMetadataEntryPath(questTag);

                    var questEntry = _archive.GetEntry(questPath);
                    
                    if(questEntry == null) return false;

                    var entriesToDelete = new List<ZipArchiveEntry>();

                    foreach(var e in _archive.Entries)
                    {
                        _linkedCT.ThrowIfCancellationRequested();

                        if(e.FullName.StartsWith(questPath)
                        && e.FullName.Length != questPath.Length
                        && (e.FullName == metadataPath || int.TryParse(e.FullName[questPath.Length..], out _)))
                            entriesToDelete.Add(e);   
                    }

                    foreach(var questData in entriesToDelete)
                        questData.Delete();

                    _archive.GetEntry(metadataPath)?.Delete();

                    questEntry.Delete();

                    return true;

                }, _linkedCT); // cancel is safe only before the first Delete operation, to avoid Zip corruption
            }
            finally
            {
                _zipLock.Release();
            }
        }

        public async Task<bool> RemoveNodeAsync(string questTag, int nodeID)
        {
            ThrowIfReadOnly();

            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                return await Task.Run(() =>
                {
                    var questPath = GetQuestEntryPath(questTag);

                    if(!_archive.Entries.Any(e=>e.FullName==questPath))
                        return false;

                    var entry = _archive.GetEntry(GetNodeEntryPath(questTag, nodeID));

                    if(entry == null) return false;

                    _linkedCT.ThrowIfCancellationRequested();

                    entry.Delete();

                    return true;

                }, _linkedCT); // cancel is safe only before the first Delete operation, to avoid Zip corruption
            }
            finally
            {
                _zipLock.Release();
            }
        }

        public async Task<bool> RemoveMetadataAsync(string questTag)
        {
            ThrowIfReadOnly();

            await _zipLock.WaitAsync(_linkedCT);

            try
            {            
                return await Task.Run(() =>
                {                    
                    var questPath = GetQuestEntryPath(questTag);

                    if(!_archive.Entries.Any(e=>e.FullName==questPath))
                        return false;

                    var entry = _archive.GetEntry(GetMetadataEntryPath(questTag));

                    if(entry == null) return false;

                    _linkedCT.ThrowIfCancellationRequested();

                    entry.Delete();

                    return true;

                }, _linkedCT);
            }
            finally
            {
                _zipLock.Release();
            }

        }
        public async Task<Quest?> GetQuestAsync(string questTag)
        {
            await _zipLock.WaitAsync(_linkedCT);
            
            try
            {  
                _linkedCT.ThrowIfCancellationRequested();

                var entry = _archive.GetEntry(GetQuestEntryPath(questTag));

                if(entry == null) return default;

                using var stream = entry.Open();

                var obj = await JsonSerializer.DeserializeAsync<Quest>(stream, IQuestDataSerializer.Options, _linkedCT);

                _linkedCT.ThrowIfCancellationRequested();

                return obj;
            }
            finally
            {
                _zipLock.Release();
            }
        }


        /// <returns>Array of nodes deserialized from the file, or null if quest does not exist in the pack.</returns>
        public async Task<Quest[]?> GetQuestsAsync()
        {
            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                _linkedCT.ThrowIfCancellationRequested();

                var quests = new List<Quest>();


                foreach (var entry in _archive.Entries.Where(e => e.FullName.EndsWith('/')))
                {

                    using var stream = entry.Open();

                    var quest = await _serializer.DeserializeQuestFromStreamAsync(stream, _linkedCT);

                    _linkedCT.ThrowIfCancellationRequested();

                    if (quest == null) return null;

                    quests.Add(quest);
                }

                return quests.ToArray();
            }
            finally
            {
                _zipLock.Release();
            }
        }

        /// <returns>Array of nodes deserialized from the file, or null if quest does not exist in the pack.</returns>
        public async Task<NodeBase[]?> GetNodesAsync(string questTag)
        {
            await _zipLock.WaitAsync(_linkedCT);

            try
            {
                _linkedCT.ThrowIfCancellationRequested();

                var nodes = new List<NodeBase>();

                string questPath = GetQuestEntryPath(questTag);
                string metadataEntryPath = GetMetadataEntryPath(questTag);

                foreach (var entry in _archive.Entries)
                {
                    if (!entry.FullName.StartsWith(questPath)
                    || entry.FullName == questPath
                    || entry.FullName == metadataEntryPath
                    || !int.TryParse(entry.FullName[questPath.Length..],out _))
                        continue;

                    using var stream = entry.Open();

                    var node = await _serializer.DeserializeNodeFromStreamAsync(stream,_linkedCT);

                    _linkedCT.ThrowIfCancellationRequested();

                    if(node == null) return null;

                    nodes.Add(node);
                }

                return nodes.ToArray();
            }
            finally
            {
                _zipLock.Release();
            }
        }

        /// <param name="options">Explicit serializer options</param>
        /// <returns>Object deserialized from the file, or null if either quest does not exist, or it does not carry any metadata.</returns>
        public async Task<T?> GetMetadataAsync<T>(string questTag, JsonSerializerOptions? options = null)
        {
            await _zipLock.WaitAsync(_linkedCT);

            try
            {  
                _linkedCT.ThrowIfCancellationRequested();

                var entry = _archive.GetEntry(GetMetadataEntryPath(questTag));

                if(entry == null) return default;

                using var stream = entry.Open();

                var obj = await JsonSerializer.DeserializeAsync<T>(stream, options, _linkedCT);

                _linkedCT.ThrowIfCancellationRequested();

                return obj;
            }
            finally
            {
                _zipLock.Release();
            }
        }

    }
}