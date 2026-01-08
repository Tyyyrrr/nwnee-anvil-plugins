using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
                
                pack.Comment = $"{fName} (modified at {lastWriteTime})";
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
            var questDir = questTag + '/';
            foreach (var pack in _packs)
            {
                if (pack.Entries.Any(e => e.FullName.StartsWith(questDir, StringComparison.OrdinalIgnoreCase)))
                    return pack;
            }
            return null;
        }

        public bool TryGetQuestImmediate(string questTag, [NotNullWhen(true)] out Quest? quest)
        {
            string questPath = $"{questTag}/q";

            quest = null;
            ZipArchiveEntry? entry = null;

            foreach (var pack in _packs)
            {
                entry = pack.GetEntry(questPath);
                if (entry != null) break;
            }

            if (entry == null) return false;

            quest = Quest.Deserialize(entry.Open());

            return quest != null;
        }

        public bool TryGetQuestStageImmediate(string questTag, int stageId, [NotNullWhen(true)] out QuestStage? stage)
        {
            stage = null;

            var pack = FindPack(questTag);

            if (pack == null) return false;

            string stagePath = $"{questTag}/{stageId}";

            var entry = pack.GetEntry(stagePath);

            if (entry == null) return false;

            stage = QuestStage.Deserialize(entry.Open());

            return stage != null;
        }
    }
}