using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

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

        public bool TryGetQuestImmediate(string questTag, [NotNullWhen(true)] out Quest? quest)
        {
            quest = null;
         
            ZipArchiveEntry? entry = FindEntryInPacks(questTag+"/q");

            if (entry == null) return false;

            quest = QuestSerializer.Deserialize<Quest>(entry.Open());

            return quest != null;
        }

        public async Task<Quest?> TryGetQuestAsync(string questTag)
        {
            ZipArchiveEntry? entry = FindEntryInPacks(questTag+"/q");

            if (entry == null) return null;

            return await QuestSerializer.DeserializeAsync<Quest>(entry.Open());
        }

        public bool TryGetQuestStageImmediate(string questTag, int stageId, [NotNullWhen(true)] out QuestStage? stage)
        {
            stage = null;
         
            ZipArchiveEntry? entry = FindEntryInPacks($"{questTag}/{stageId}");

            if (entry == null) return false;

            stage = QuestSerializer.Deserialize<QuestStage>(entry.Open());

            return stage != null;
        }

        public async Task<QuestStage?> TryGetQuestStageAsync(string questTag, int stageId)
        {
            ZipArchiveEntry? entry = FindEntryInPacks($"{questTag}/{stageId}");

            if (entry == null) return null;

            return await QuestSerializer.DeserializeAsync<QuestStage>(entry.Open());
        }
    }
}