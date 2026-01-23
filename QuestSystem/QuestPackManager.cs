using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace QuestSystem
{
    internal sealed class QuestPackManager : IDisposable
    {
        private readonly RuntimeQuestPack[] _packs;

        public QuestPackManager(string directory)
        {
            var list = new List<RuntimeQuestPack>();
            string packsInfoStr = "";

            foreach (var fPath in Directory.GetFiles(directory))
            {
                var fExt = Path.GetExtension(fPath);

                if (fExt != QuestPack.FileExtension) continue;

                var lastWriteTime = File.GetLastWriteTime(fPath);
                var fName = Path.GetFileNameWithoutExtension(fPath);

                var stream = File.OpenRead(fPath);
                var pack = new RuntimeQuestPack(stream);
                list.Add(pack);
                
                packsInfoStr += $"\n - {fName} (last modified: {lastWriteTime})";
            }

            _packs = list.ToArray();

            string str = "Loaded " + _packs.Length + " quest packs:" + packsInfoStr;

            NLog.LogManager.GetCurrentClassLogger().Info(str);
        }

        public bool TryGetQuestImmediate(string questTag, [NotNullWhen(true)] out Quest? quest)
        {
            foreach(var pack in _packs)
            {
                quest = pack.GetQuest(questTag);

                if(quest != null) return true;
            }
            quest = null;
            return false;
        }
        
        public void Dispose()
        {
            foreach (var pack in _packs)
                pack.Dispose();
        }

    }
}