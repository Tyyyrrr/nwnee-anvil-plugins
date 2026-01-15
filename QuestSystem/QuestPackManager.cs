using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using QuestSystem.Graph;
using QuestSystem.Nodes;
using QuestSystem.Wrappers;

namespace QuestSystem
{
    internal sealed class QuestPackManager : IDisposable
    {
        private readonly QuestPack[] _packs;

        public QuestPackManager(string directory)
        {
            var list = new List<QuestPack>();

            foreach (var fPath in Directory.GetFiles(directory))
            {
                var fExt = Path.GetExtension(fPath);

                if (fExt != QuestPack.FileExtension) continue;

                var lastWriteTime = File.GetLastWriteTime(fPath);
                var fName = Path.GetFileNameWithoutExtension(fPath);

                var pack = QuestPack.OpenRead(fPath);
                list.Add(pack);
                
                pack.Comment = $"{fName} (last modified: {lastWriteTime})";
            }

            _packs = list.ToArray();

            string str = "Loaded " + _packs.Length + " quest packs:";
            foreach(var pack in _packs)
            {
                str += $"\n{pack.Comment}\n - Quests:";

                foreach(var entry in pack.Entries.Where(e => e.FullName.EndsWith("/q")))
                {
                    var s = new string(entry.FullName.Take(entry.FullName.Length-2).ToArray());

                    str += $"\n - - {s}";
                }
            }

            NLog.LogManager.GetCurrentClassLogger().Info(str);
        }

        public void Dispose()
        {
            foreach (var pack in _packs)
                pack.Dispose();
        }

        public QuestPack? FindPack(string questTag)
        {
            var questPath = questTag + "/q";
            foreach (var pack in _packs)
            {
                if(pack.GetEntry(questPath) != null)
                    return pack;
            }
            return null;
        }

        private ZipArchiveEntry? FindEntryInPacks(string entryPath)
        {
            foreach(var pack in _packs)
            {
                var entry = pack.GetEntry(entryPath);
                if(entry != null) return entry;
            }
            return null;
        }

        internal bool TryGetQuestImmediate(string questTag, [NotNullWhen(true)] out Quest? quest)
        {
            quest = null;
         
            ZipArchiveEntry? entry = FindEntryInPacks(questTag+"/q");

            if (entry == null) return false;

            quest = QuestSerializer.Deserialize<Quest>(entry.Open());

            if(quest != null)
            {
                quest.Pack = entry.Archive as QuestPack;
                return true;
            }

            return false;
        }

        public async Task<Quest?> TryGetQuestAsync(string questTag)
        {
            ZipArchiveEntry? entry = FindEntryInPacks(questTag+"/q");

            if (entry == null) return null;

            return await QuestSerializer.DeserializeAsync<Quest>(entry.Open());
        }

        internal bool TryGetNodeImmediate<T>(Quest quest, int nodeId, [NotNullWhen(true)] out T? node) where T : class, INode
        {
            node = null;

            var pack = quest.Pack;
            if(pack == null) return false;

            var entry = pack.GetEntry($"{quest.Tag}/{nodeId}");
            if(entry == null) return false;
            
            node = QuestSerializer.Deserialize<T>(entry.Open());
            return node != null;
        }


        public async Task<StageNode?> TryGetQuestStageAsync(string questTag, int stageId)
        {
            ZipArchiveEntry? entry = FindEntryInPacks($"{questTag}/{stageId}");

            if (entry == null) return null;

            return await QuestSerializer.DeserializeAsync<StageNode>(entry.Open());
        }
    }
}