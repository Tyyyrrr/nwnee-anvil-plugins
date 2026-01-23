using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace QuestSystem
{
    /// <summary>
    /// Facade for interacting with .zip archive I/O
    /// </summary>
    public abstract class QuestPack : IDisposable
    {
        protected readonly ZipArchive _archive;

        public static readonly string FileExtension = ".qp";

        public static string GetQuestEntryPath(string questTag) => $"{questTag}/";
        public static string GetNodeEntryPath(string questTag, int nodeID) => $"{questTag}/{nodeID}";
        public static string GetMetadataEntryPath(string questTag) => $"{questTag}/.";
        
        internal static readonly QuestDataSerializer DefaultSerializer = new();

        protected QuestPack(Stream stream, bool readOnly)
        {
            _archive = new ZipArchive(stream, readOnly ? ZipArchiveMode.Read : ZipArchiveMode.Update, false, Encoding.UTF8);
        }

        public sealed class ReadOnlyException : Exception
        {
            private static readonly string _msg = $"{nameof(QuestPack)} is open in read-only mode. Write access is prohibited.";
            internal ReadOnlyException() : base(_msg) { }
        }
        protected void ThrowIfReadOnly() { if (_archive.Mode == ZipArchiveMode.Read) ThrowReadOnly(); }
        [DoesNotReturn] private static void ThrowReadOnly() => throw new ReadOnlyException();

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _archive.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}