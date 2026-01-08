using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestSystem
{
    public sealed class QuestPack : ZipArchive, IDisposable
    {
        private readonly bool _readOnly = false;

        public static readonly string FileExtension = ".qp";


        public sealed class ReadOnlyException : Exception
        {
            private static readonly string _msg = $"{nameof(QuestPack)} is open in read-only mode. Write access is prohibited.";
            internal ReadOnlyException() : base(_msg) { }
        }
        public void ThrowIfReadOnly() { if (_readOnly) ThrowReadOnly(); }
        [DoesNotReturn] private static void ThrowReadOnly() => throw new ReadOnlyException();



        public static QuestPack OpenRead(string path)
        {
            var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
            return new QuestPack(stream, true);
        }

        public static QuestPack OpenWrite(string path)
        {
            var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return new QuestPack(stream, false);
        }

        private QuestPack(Stream stream, bool readOnly) : base(stream, readOnly ? ZipArchiveMode.Read : ZipArchiveMode.Update, false, Encoding.UTF8) { _readOnly = readOnly; }


        public async Task<Quest?> GetQuestAsync(string questTag)
        {
            string questPath = $"{questTag}/q";

            NLog.LogManager.GetCurrentClassLogger().Warn("Getting quest at path " + questPath);

            var entry = Entries.FirstOrDefault(e => string.Equals(e.FullName, questPath, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return null;

            using var sr = new StreamReader(entry.Open());
            var json = await sr.ReadToEndAsync();
            return Quest.Deserialize(json);
        }


        public async Task<bool> AddQuestAsync(Quest quest)
        {
            ThrowIfReadOnly();

            string questFolder = quest.Tag + '/';

            if (Entries.Any(e => e.FullName.StartsWith(questFolder, StringComparison.OrdinalIgnoreCase)))
            {
                // todo: add logging
                return false;
            }

            var json = Quest.Serialize(quest);

            var entry = CreateEntry($"{questFolder}q");

            await using var sw = new StreamWriter(entry.Open());
            await sw.WriteAsync(json);
            return true;
        }

        public bool TryGetStageImmediate(string questTag, int stageID, [NotNullWhen(true)] out QuestStage? stage)
        {
            var stagePath = $"{questTag}/{stageID}";

            var entry = GetEntry(stagePath);

            stage = entry == null ? null : QuestStage.Deserialize(entry.Open());

            return stage != null;
        }

        public async Task<QuestStage?> GetStageAsync(string questTag, int stageID)
        {
            var stagePath = $"{questTag}/{stageID}";

            NLog.LogManager.GetCurrentClassLogger().Warn("Getting stage at path " + stagePath);

            var entry = Entries.FirstOrDefault(e => string.Equals(e.FullName, stagePath));

            if (entry == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("NO ENTRY");
                return null;
            }

            using var sr = new StreamReader(entry.Open());
            var json = await sr.ReadToEndAsync();

            var stage = QuestStage.Deserialize(json);
            if (stage == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("DESERIALIZATION FAILED");
            }

            return stage;
        }
        public async Task<bool> SetStageAsync(string questTag, QuestStage stage)
        {
            ThrowIfReadOnly();

            string questFolder = questTag + '/';

            if (!Entries.Any(e => e.FullName.StartsWith(questFolder, StringComparison.OrdinalIgnoreCase)))
            {
                // todo: add logging
                return false;
            }

            string stagePath = $"{questFolder}{stage.ID}";

            var json = QuestStage.Serialize(stage);

            var entry = Entries.FirstOrDefault(e => e.FullName.StartsWith(stagePath, StringComparison.OrdinalIgnoreCase));

            if (entry != default) entry.Delete();

            entry = CreateEntry(stagePath);

            await using var sw = new StreamWriter(entry.Open());
            await sw.WriteAsync(json);
            return true;
        }

        public bool RemoveStage(string questTag, int stageID)
        {
            ThrowIfReadOnly();

            string stagePath = $"{questTag}/{stageID}";

            var entry = Entries.FirstOrDefault(e => e.FullName.StartsWith(stagePath));

            if (entry == null) return false;

            entry.Delete();

            return true;
        }
    }
}