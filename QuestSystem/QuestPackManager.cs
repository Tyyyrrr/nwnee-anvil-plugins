using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuestSystem
{
    public static class QuestPackManager
    {
        private static QuestPack[] _packs = Array.Empty<QuestPack>();
        public static void GetPacks(string directory)
        {
            foreach(var pack in _packs)
                pack.Dispose();

            var list = new List<QuestPack>();

            foreach(var fInfo in Directory.GetFiles(directory))
            {
                var fExt = Path.GetExtension(fInfo);

                if(fExt != QuestPack.FileExtension) continue;

                var pack = QuestPack.OpenRead(fInfo);
                list.Add(pack);
                _packs = list.ToArray();
            }
        }

        public static QuestPack? FindPack(string questTag)
        {
            var questDir = questTag+'/';
            foreach(var pack in _packs)
            {
                if(pack.Entries.Any(e=>e.FullName.StartsWith(questDir, StringComparison.OrdinalIgnoreCase)))
                    return pack;
            }
            return null;
        }
    }
}