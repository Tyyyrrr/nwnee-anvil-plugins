using QuestSystem;
using QuestSystem.Nodes;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace QuestEditor.Explorer
{
    public sealed class PackManager : IDisposable, IAsyncDisposable
    {
        private readonly string _originalFilePath;
        private readonly string _temporaryFilePath;

        private EditorQuestPack? questPack = null;

        public event Action<Quest[]?>? QuestsLoadCompleted;
        public event Action<Quest?>? QuestReloadCompleted;
        public event Action<string, NodeBase[]?>? NodesLoadCompleted;
        public event Action<string, NodeBase?>? NodeReloadCompleted;
        public event Action<string, object?>? MetadataReadCompleted;

        private long _pendingAllQuestsLoads = 0;
        private readonly ConcurrentQueue<string> _pendingQuestsReloads = [];
        private readonly ConcurrentQueue<string> _pendingNodesLoads = [];
        private readonly ConcurrentQueue<(string, int)> _pendingNodesReloads = [];
        private readonly ConcurrentQueue<string> _pendingMetadataReads = [];

        private readonly ConcurrentQueue<string> _pendingQuestRemovals = [];
        private readonly ConcurrentQueue<(string, int)> _pendingNodeRemovals = [];
        private readonly ConcurrentQueue<string> _pendingMetadataRemovals = [];
        private readonly ConcurrentQueue<Quest> _pendingQuestWrites = [];
        private readonly ConcurrentQueue<(Quest, NodeBase)> _pendingNodeWrites = [];
        private readonly ConcurrentQueue<(Quest, string)> _pendingMetadataWrites = [];

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private Task? writingTask = null;
        private Task? readingTask = null;
        private Task? copyTask = null;

        private readonly CancellationTokenSource _cts = new();

        private bool _disposed = false;

        public PackManager(string filePath)
        {
            _originalFilePath = filePath;
            if (!File.Exists(_originalFilePath))
                throw new InvalidOperationException("Original pack file does not exist.");

            _temporaryFilePath = filePath + ".tmp";
            if (File.Exists(_temporaryFilePath))
                throw new InvalidOperationException("Temporary pack file already exists");

            File.Copy(_originalFilePath, _temporaryFilePath, true);
        }

        private async Task CopyTask(string sourcePath, string destinationPath)
        {
            _cts.Token.ThrowIfCancellationRequested();
            await _semaphore.WaitAsync(_cts.Token);
            _cts.Token.ThrowIfCancellationRequested();

            try
            {
                _cts.Token.ThrowIfCancellationRequested();

                questPack?.Dispose();

                File.Copy(sourcePath, destinationPath, true);
            }
            finally
            {
                questPack?.Dispose();
                questPack = null;
                _semaphore.Release();
            }
        }

        // -----------------------------
        // ASYNC DISPOSAL
        // -----------------------------
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();

            try
            {
                if (readingTask != null)
                    await readingTask;

                if (writingTask != null)
                    await writingTask;

                if (copyTask != null)
                    await copyTask;
            }
            catch (OperationCanceledException)
            {
                // expected during shutdown
            }

            questPack?.Dispose();
            _semaphore.Dispose();
            _cts.Dispose();

            File.Delete(_temporaryFilePath);

            Trace.WriteLine($"PackManager {_originalFilePath} disposed.");
        }

        // Synchronous fallback for safety
        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            Trace.WriteLine($"PackManager {_originalFilePath} disposed in emergency mode!");
        }


        // -----------------------------
        // READ PIPELINE
        // -----------------------------
        private async Task ReadingTask()
        {
            _cts.Token.ThrowIfCancellationRequested();
            await _semaphore.WaitAsync(_cts.Token);
            _cts.Token.ThrowIfCancellationRequested();

            try
            {
                _cts.Token.ThrowIfCancellationRequested();

                questPack?.Dispose();
                questPack = EditorQuestPack.OpenRead(
                    File.Open(_temporaryFilePath, FileMode.Open, FileAccess.Read, FileShare.None));

                while (HasPendingReads())
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    ProcessNextReadOperation();
                }
            }
            finally
            {
                questPack?.Dispose();
                questPack = null;
                _semaphore.Release();
            }
        }
        private void ProcessNextReadOperation()
        {
            if (questPack == null)
                throw new InvalidOperationException("Quest pack is null");
        
            if(Interlocked.Read(in _pendingAllQuestsLoads) > 0)
            {
                var quests = questPack.GetQuests();
                if (quests == null)
                    Trace.WriteLine("Failed to load quests from the pack.");
                else Trace.WriteLine("Loaded quests from the pack");
                Interlocked.Decrement(ref _pendingAllQuestsLoads);
                QuestsLoadCompleted?.Invoke(quests);
            }
            else if (!_pendingQuestsReloads.IsEmpty)
            {
                if (!_pendingQuestsReloads.TryDequeue(out var tag))
                    throw new InvalidOperationException("Failed to dequeue quest tag to reload");

                var quest = questPack.GetQuest(tag);
                if (quest == null)
                    Trace.WriteLine($"Failed to reload quest {tag}");
                else Trace.WriteLine($"Quest {tag} reloaded");
                QuestReloadCompleted?.Invoke(quest);
            }
            else if(!_pendingNodesLoads.IsEmpty)
            {
                if (!_pendingNodesLoads.TryDequeue(out var tag))
                    throw new InvalidOperationException("Failed to dequeue quest tag for nodes to load");
                var nodes = questPack.GetNodes(tag);
                if (nodes == null)
                    Trace.WriteLine($"Failed to load nodes for the quest {tag}");
                else Trace.WriteLine($"Nodes loaded for quest {tag}");
                NodesLoadCompleted?.Invoke(tag, nodes);
            }
            else if (!_pendingNodesReloads.IsEmpty)
            {
                if (!_pendingNodesReloads.TryDequeue(out var tuple))
                    throw new InvalidOperationException("Failed to dequeue quest tag and node ID for node to reload");
                var node = questPack.GetNode(tuple.Item1, tuple.Item2);
                if (node == null)
                    Trace.WriteLine($"Failed to reload node {tuple.Item2} of quest {tuple.Item1}");
                else Trace.WriteLine($"Reloaded node {tuple.Item2} of quest {tuple.Item1}");
                NodeReloadCompleted?.Invoke(tuple.Item1, node);
            }
            else //if (!_pendingMetadataReads.IsEmpty)
            {
                if (!_pendingMetadataReads.TryDequeue(out var tag))
                    throw new InvalidOperationException("Failed to dequeue quest tag for metadata load");
                var metadata = questPack.GetMetadata<object>(tag);
                if (metadata == null)
                    Trace.WriteLine($"Failed to load metadata of quest {tag}");
                else Trace.WriteLine($"Metadata loaded for quest {tag}");
                MetadataReadCompleted?.Invoke(tag, metadata);
            }

        }

        private bool HasPendingReads() =>
            Interlocked.Read(in _pendingAllQuestsLoads) > 0 ||
            !_pendingQuestsReloads.IsEmpty ||
            !_pendingNodesLoads.IsEmpty ||
            !_pendingNodesReloads.IsEmpty ||
            !_pendingMetadataReads.IsEmpty;

        private void EnsureReadTaskRunning()
        {
            readingTask ??= Task.Run(async () =>
            {
                try { await ReadingTask(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Trace.WriteLine(ex); }
                finally { readingTask = null; }
            });
        }

        // -----------------------------
        // WRITE PIPELINE
        // -----------------------------
        private async Task WritingTask()
        {
            _cts.Token.ThrowIfCancellationRequested();
            await _semaphore.WaitAsync(_cts.Token);
            _cts.Token.ThrowIfCancellationRequested();

            try
            {
                _cts.Token.ThrowIfCancellationRequested();

                questPack?.Dispose();
                questPack = EditorQuestPack.OpenWrite(
                    File.Open(_temporaryFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None));

                while (HasPendingWrites())
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    ProcessNextWriteOperation();
                }
            }
            finally
            {
                questPack?.Dispose();
                questPack = null;
                _semaphore.Release();
            }
        }

        private void ProcessNextWriteOperation()
        {
            if (questPack == null)
                throw new InvalidOperationException("Quest pack is null");

            if (!_pendingQuestRemovals.IsEmpty)
            {
                if (!_pendingQuestRemovals.TryDequeue(out var tag))
                    throw new InvalidOperationException("Failed to dequeue quest tag to remove");

                if (!questPack.RemoveQuest(tag))
                    Trace.WriteLine($"Failed to remove quest {tag}");
                else Trace.WriteLine($"Quest {tag} removed.");
            }
            else if (!_pendingNodeRemovals.IsEmpty)
            {
                if (!_pendingNodeRemovals.TryDequeue(out var tuple))
                    throw new InvalidOperationException("Failed to dequeue node to remove");

                if (!questPack.RemoveNode(tuple.Item1, tuple.Item2))
                    Trace.WriteLine($"Failed to remove node {tuple}");
                else Trace.WriteLine($"Node {tuple.Item2} removed from quest {tuple.Item1}.");
            }
            else if (!_pendingMetadataRemovals.IsEmpty)
            {
                if (!_pendingMetadataRemovals.TryDequeue(out var tag))
                    throw new InvalidOperationException("Failed to dequeue metadata to remove");

                if (!questPack.RemoveMetadata(tag))
                    Trace.WriteLine($"Failed to remove metadata {tag}");
                else Trace.WriteLine($"Metadata removed from quest {tag}.");
            }
            else if (!_pendingQuestWrites.IsEmpty)
            {
                if (!_pendingQuestWrites.TryDequeue(out var quest))
                    throw new InvalidOperationException("Failed to dequeue quest to write");

                if (!questPack.WriteQuest(quest))
                    Trace.WriteLine($"Failed to write quest {quest.Tag}");
                else Trace.WriteLine($"Quest {quest} written to the archive.");
            }
            else if (!_pendingNodeWrites.IsEmpty)
            {
                if (!_pendingNodeWrites.TryDequeue(out var tuple))
                    throw new InvalidOperationException("Failed to dequeue node to write");

                if (!questPack.WriteNode(tuple.Item1, tuple.Item2))
                    Trace.WriteLine($"Failed to write node {tuple}");
                else Trace.WriteLine($"Node {tuple.Item2.ID} of quest {tuple.Item1} written to the archive.");
            }
            else //if(!_pendingMetadataWrites.IsEmpty)
            {
                if (!_pendingMetadataWrites.TryDequeue(out var tuple))
                    throw new InvalidOperationException("Failed to dequeue metadata to write");

                if (!questPack.WriteMetadata(tuple.Item1.Tag, tuple.Item2))
                    Trace.WriteLine($"Failed to write metadata for {tuple.Item1}");
                else Trace.WriteLine($"Metadata for quest {tuple.Item1} written to the archive.");
            }
        }

        private bool HasPendingWrites() =>
            !_pendingQuestRemovals.IsEmpty ||
            !_pendingNodeRemovals.IsEmpty ||
            !_pendingMetadataRemovals.IsEmpty ||
            !_pendingQuestWrites.IsEmpty ||
            !_pendingNodeWrites.IsEmpty ||
            !_pendingMetadataWrites.IsEmpty;

        private void EnsureWriteTaskRunning()
        {
            writingTask ??= Task.Run(async () =>
            {
                try { await WritingTask(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Trace.WriteLine(ex); }
                finally { writingTask = null; }
            });
        }



        // -----------------------------
        // PUBLIC API
        // -----------------------------

        // READS:
        public void DiscardChanges()
        {
            copyTask ??= Task.Run(async () =>
            {
                try { await CopyTask(_originalFilePath, _temporaryFilePath); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Trace.WriteLine(ex); }
                finally { copyTask = null; }
            });
        }

        public void LoadAllQuests()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (Interlocked.Read(in _pendingAllQuestsLoads) > 0) return;

            _ = Interlocked.Increment(ref _pendingAllQuestsLoads);

            EnsureReadTaskRunning();
        }

        public void ReloadQuest(string tag)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingQuestsReloads.Any(s=>s == tag)) return;

            _pendingQuestsReloads.Enqueue(tag);

            EnsureReadTaskRunning();
        }

        public void LoadAllNodes(Quest quest)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if(_pendingNodesLoads.Any(s=>s == quest.Tag)) return;

            _pendingNodesLoads.Enqueue(quest.Tag);

            EnsureReadTaskRunning();
        }

        public void ReloadNode(Quest quest, int nodeID)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if(_pendingNodesReloads.Any(pnr=>pnr.Item1 == quest.Tag && pnr.Item2 == nodeID)) return;

            _pendingNodesReloads.Enqueue((quest.Tag, nodeID));

            EnsureReadTaskRunning();
        }

        public void LoadMetadata(Quest quest)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingMetadataReads.Any(s => s == quest.Tag)) return;

            _pendingMetadataReads.Enqueue(quest.Tag);

            EnsureReadTaskRunning();
        }

        // WRITES:
        public void ApplyChanges()
        {
            copyTask ??= Task.Run(async () =>
            {
                try { await CopyTask(_temporaryFilePath, _originalFilePath); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Trace.WriteLine(ex); }
                finally { copyTask = null; }
            });
        }

        public void WriteQuest(Quest quest)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingQuestWrites.Any(p => p.Tag == quest.Tag)) return;

            _pendingQuestWrites.Enqueue(quest);
            EnsureWriteTaskRunning();
        }

        public void RemoveQuest(string tag)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingQuestRemovals.Contains(tag)) return;

            _pendingQuestRemovals.Enqueue(tag);
            EnsureWriteTaskRunning();
        }

        public void WriteNode(Quest quest, NodeBase node)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingNodeWrites.Any(p => p.Item1.Tag == quest.Tag && p.Item2.ID == node.ID)) return;

            _pendingNodeWrites.Enqueue((quest, node));
            EnsureWriteTaskRunning();
        }

        public void RemoveNode(string tag, int id)
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingNodeRemovals.Any(p => p.Item1 == tag && p.Item2 == id)) return;

            _pendingNodeRemovals.Enqueue((tag, id));
            EnsureWriteTaskRunning();
        }

        public void WriteMetadata(Quest quest, string serializedMetadata)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingMetadataWrites.Any(p => p.Item1.Tag == quest.Tag && p.Item2 == serializedMetadata)) return;

            _pendingMetadataWrites.Enqueue((quest, serializedMetadata));
            EnsureWriteTaskRunning();
        }

        public void RemoveMetadata(string tag)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cts.IsCancellationRequested) return;

            if (_pendingMetadataRemovals.Contains(tag)) return;

            _pendingMetadataRemovals.Enqueue(tag);
            EnsureWriteTaskRunning();
        }
    }

}