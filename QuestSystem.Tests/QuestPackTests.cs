using QuestSystem.Nodes;

namespace QuestSystem.Tests;

[TestFixture(TestOf = typeof(EditorQuestPack))]
public class EditorQuestPackTests
{
    private sealed class MockSerializer : IQuestDataSerializer
    {
        public Task<NodeBase?> DeserializeNodeFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Quest?> DeserializeQuestFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SerializeToStreamAsync(Quest quest, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task SerializeToStreamAsync(NodeBase node, Stream stream)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NonClosingStream : Stream
    {
        private readonly Stream _inner;

        public NonClosingStream(Stream inner) => _inner = inner;

        protected override void Dispose(bool disposing)
        {
            // swallow dispose so underlying stream stays open
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
    }


    private IQuestDataSerializer serializer;
    private static CancellationTokenSource? _cts;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        serializer = new MockSerializer();
    }

    [SetUp]
    public void Setup()
    {
        _cts?.Dispose();
        _cts = new();   
    }

    [Test]
    public void EditorPackOpenWrite_WriteableStream_NeverThrows()
    {
        var stream = new MemoryStream();

        Assert.DoesNotThrow(()=>{EditorQuestPack.OpenWrite(stream,serializer,_cts!.Token);});
    }
        
    [Test]
    public void EditorPackOpenWrite_ReadOnlyStream_ThrowsArgumentException()
    {
        var readonlyStream = new MemoryStream(new byte[1],false);

        Assert.Throws<ArgumentException>(()=>{EditorQuestPack.OpenWrite(readonlyStream,serializer,_cts!.Token);});
    }


    [Test]
    public void EditorPack_OpenWriteThenDispose_DisposesUnderlyingStream()
    {
        var stream = new MemoryStream();
        var qp = EditorQuestPack.OpenWrite(stream, serializer, _cts!.Token);
        qp.Dispose();
        Assert.Throws<ObjectDisposedException>(()=>stream.Write([1]));
    }

    [Test]
    public void EditorPack_WriteEmptyPack_CreatesReadableArchive()
    {
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        var wrQP = EditorQuestPack.OpenWrite(nonClosingStream, serializer, _cts!.Token);

        wrQP.Dispose();

        nonClosingStream.Flush();
        nonClosingStream.Seek(0, SeekOrigin.Begin);
        var roStream = new MemoryStream();
        nonClosingStream.CopyTo(roStream);

        Assert.DoesNotThrow(()=>EditorQuestPack.OpenRead(roStream, serializer, _cts!.Token).Dispose());
        stream.Dispose();
    }
}