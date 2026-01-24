using QuestSystem.Nodes;
using QuestSystem.Objectives;
using QuestSystem.Wrappers;
using System.Text.Json;

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


    private IQuestDataSerializer? _serializer;
    private CancellationTokenSource? _cts;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _serializer = new MockSerializer();
    }

    [SetUp]
    public void Setup()
    {
        _cts?.Dispose();
        _cts = new();   
    }

    [Test]
    public void EditorPackOpenWrite_EmptyWriteableStream_NeverThrows()
    {
        var stream = new MemoryStream();
        Assert.DoesNotThrow(()=> EditorQuestPack.OpenWrite(stream, _serializer, _cts!.Token));

        stream.Dispose();
    }
        
    [Test]
    public void EditorPackOpenWrite_ReadOnlyStream_ThrowsArgumentException()
    {
        var readonlyStream = new MemoryStream(new byte[1],false);

        Assert.Throws<ArgumentException>(()=>{EditorQuestPack.OpenWrite(readonlyStream, _serializer, _cts!.Token);});
    }


    [Test]
    public void EditorPack_OpenWriteThenDispose_DisposesUnderlyingStream()
    {
        var stream = new MemoryStream();
        var qp = EditorQuestPack.OpenWrite(stream, _serializer, _cts!.Token);
        qp.Dispose();
        Assert.Throws<ObjectDisposedException>(()=>stream.Write([1]));
    }

    [Test]
    public void EditorPack_OpenReadThenDispose_DisposesUnderlyingStream()
    {
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        var wrQP = EditorQuestPack.OpenWrite(nonClosingStream, _serializer, _cts!.Token);
        wrQP.Dispose();

        var qp = EditorQuestPack.OpenWrite(stream, _serializer, _cts!.Token);
        qp.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Test]
    public void EditorPack_WriteEmptyPack_CreatesReadableArchive()
    {
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        var wrQP = EditorQuestPack.OpenWrite(nonClosingStream, _serializer, _cts!.Token);
        wrQP.Dispose();

        nonClosingStream.Flush();
        nonClosingStream.Seek(0, SeekOrigin.Begin);
        var roStream = new MemoryStream();
        nonClosingStream.CopyTo(roStream);

        Assert.DoesNotThrow(()=>EditorQuestPack.OpenRead(roStream, _serializer, _cts!.Token).Dispose());
        stream.Dispose();
    }

    [Test]
    public async Task EditorPack_WriteQuest_ReadsTheSameQuest()
    {
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);
        var quest = new Quest() { Tag = "test1", Name = "test1 name" };

        using (var qpWO = EditorQuestPack.OpenWrite(nonClosingStream))
        {
            Assert.That(await qpWO.WriteQuestAsync(quest));
        }

        using var qpRO = EditorQuestPack.OpenRead(stream);

        var actual = await qpRO.GetQuestAsync(quest.Tag);

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual!.Tag == "test1" && actual.Name=="test1 name", Is.True);
    }

    private static NodeBase[] GetNodes() =>
    [
        new UnknownNode("corrupted json"),
        new StageNode(){ID=1,JournalEntry="stage joudnal entry", NextID=2, ShowInJournal=true,
            Objectives=[
                new ObjectiveKill(){Amount=2, ResRef="someresref", AreaTags=["areatag1","areatag2"]},
                new ObjectiveInteract(){Interaction=ObjectiveInteract.InteractionType.PlaceableUse}
                ]
        },
        new RewardNode(){ID=3, Gold=25, Xp=10,Items=new(){ { "resref", 2 } } },
        new RandomizerNode(){ID=4, Branches=new(){
            {5,.1f },
            {6,.9f }
        }}
    ];

    private string CompareValueTypePropertiesOfNodes(NodeBase left, NodeBase right)
    {
        if(left.GetType() != right.GetType()) return $"Different types: {left.GetType().Name} / {right.GetType().Name}";

        foreach(var p in left.GetType().GetProperties())
        {
            if (!p.PropertyType.IsValueType) continue;
            var leftValue = p.GetValue(left); 
            var rightValue = p.GetValue(right); 
            if (!Equals(leftValue, rightValue)) return $"Different values of property {p.Name}: {leftValue} / {rightValue}";
        }
        return string.Empty;
    }

    [TestCaseSource(nameof(GetNodes))]
    public async Task EditorPack_DefaultSerializer_WriteNodeThenReadNode_ReadsTheSameNode(NodeBase node)
    {
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        var qpWO = EditorQuestPack.OpenWrite(nonClosingStream, null, _cts!.Token);
        var quest = new Quest() { Tag = "testquest", Name = "Some Title", Pack = qpWO };
        await qpWO.WriteQuestAsync(new Quest() { Tag = "testquest", Name = "Some Title", Pack = qpWO });
        await qpWO.WriteNodeAsync(quest, node);
        qpWO.Dispose();

        var qpRO = EditorQuestPack.OpenRead(stream, null, _cts!.Token);
        var deserializedNodes = await qpRO.GetNodesAsync(quest.Tag);
        qpRO.Dispose();

        var disp = Assert.EnterMultipleScope();

        Assert.That(deserializedNodes, Is.Not.Null);

        NodeBase? dNode = null;
        foreach(var dnode in deserializedNodes!)
        {
            if(node.ID == dnode.ID)
            {
                dNode = dnode;
                break;
            }
        }

        Assert.That(dNode, Is.Not.Null);

        string str = CompareValueTypePropertiesOfNodes(dNode!, node);
        Assert.That(str, Is.Empty);

        disp.Dispose();
    }


    private sealed class CorruptedNode : NodeBase
    {
        public override int ID { get; set; } = 2000;
        public string SomeString { get; set; } = "CORRUPTED NODE";
        internal override WrapperBase Wrap()
        {
            throw new NotImplementedException();
        }
    }

    [Test]
    public async Task EditorPack_DefaultSerializer_CorruptedNode_WritesUnknownNode()
    {
        var quest = new Quest() { Tag = "questTag" };
        var corruptedNode = new CorruptedNode();
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        var qpWO = EditorQuestPack.OpenWrite(nonClosingStream, null, _cts!.Token);
        await qpWO.WriteQuestAsync(quest);
        await qpWO.WriteNodeAsync(quest, corruptedNode);
        qpWO.Dispose();

        var qpRO = EditorQuestPack.OpenRead(stream, null, _cts!.Token);
        var nodes = await qpRO.GetNodesAsync(quest.Tag);
        qpRO.Dispose();
        var node = nodes![0];

        if (node is UnknownNode uNode)
            TestContext.Out.WriteLine("deserialized node raw: " + uNode.RawData);
        Assert.That(node, Is.TypeOf<UnknownNode>());

    }

    [Test(ExpectedResult = false)]
    public async Task<bool> EditorPack_DefaultSerializer_MissingQuest_RefuseToWriteNode()
    {
        var quest = new Quest() { Tag = "not in the pack" };
        var stream = new MemoryStream();
        using var qpWO = EditorQuestPack.OpenWrite(stream, null, _cts!.Token);
        return await qpWO.WriteNodeAsync(quest, new StageNode());
    }

    [Test(ExpectedResult = true)]
    public async Task<bool> EditorPack_DefaultSerializer_DuplicatedQuest_RefuseToWriteQuest()
    {
        var quest = new Quest() { Tag = "test" };
        var stream = new MemoryStream();
        using var qpWO = EditorQuestPack.OpenWrite(stream, null, _cts!.Token);
        bool firstWrite = await qpWO.WriteQuestAsync(quest);
        bool secondWrite = await qpWO.WriteQuestAsync(quest);
        return firstWrite && !secondWrite;
    }

    [Test(ExpectedResult = true)]
    public async Task<bool> EditorPack_DefaultSerializer_DuplicatedNodeID_RefuseToWriteNode()
    {
        var quest = new Quest() { Tag = "test" };
        var stream = new MemoryStream();
        using var qpWO = EditorQuestPack.OpenWrite(stream, null, _cts!.Token);
        bool questWrite = await qpWO.WriteQuestAsync(quest);
        bool firstWrite = await qpWO.WriteNodeAsync(quest, new StageNode() { ID = 1 });
        bool secondWrite = await qpWO.WriteNodeAsync(quest, new StageNode() { ID = 2 });
        bool thirdWrite = await qpWO.WriteNodeAsync(quest, new RewardNode() { ID = 1 });
        return questWrite && firstWrite && secondWrite && !thirdWrite;
    }


    private sealed class MockMetadata
    {
        public string SomeString { get; set; } = "Some string";
    }

    [Test(ExpectedResult = false)]
    public async Task<bool> EditorPack_MissingQuest_RefuseToWriteMetadata()
    {
        var metadata = new MockMetadata();
        var json = JsonSerializer.Serialize(metadata);

        var stream = new MemoryStream();
        using var qpWO = EditorQuestPack.OpenWrite(stream, null, _cts!.Token);
        return await qpWO.WriteMetadataAsync("missingQuestTag", json);
    }

    [Test(ExpectedResult = true)]
    public async Task<bool> EditorPack_MetadataExists_RefuseToOverwriteMetadata()
    {
        var quest = new Quest() { Tag = "testQuest" };
        var metadata = new MockMetadata();
        var json = JsonSerializer.Serialize(metadata);

        var stream = new MemoryStream();
        using var qpWO = EditorQuestPack.OpenWrite(stream, null, _cts!.Token);
        var questWrite = await qpWO.WriteQuestAsync(quest);
        var firstWrite = await qpWO.WriteMetadataAsync(quest.Tag, json);
        return !(await qpWO.WriteMetadataAsync("missingQuestTag", json)) && questWrite && firstWrite;
    }

    [Test]
    public async Task EditorPack_ComplexRoundTrip()
    {
        var questAMetadata = new MockMetadata() { SomeString = "QuestA metadata" };
        var questBMetadata = new MockMetadata() { SomeString = "QuestB metadata" };

        var questA = new Quest() { Tag = "questA", Name = "First quest title" };
        var questB = new Quest() { Tag = "questB", Name = "Second quest title" };

        var stage1 = new StageNode() { ID = 0, NextID = 1, JournalEntry = "Stage1 journal entry" };
        var stage2 = new StageNode() { ID = 1, NextID = 2, JournalEntry = "Stage2 journal entry" };
        var reward1 = new RewardNode() { ID = 1, Gold = 123 };
        var reward2 = new RewardNode() { ID = 2, Gold = 100 };

        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        using (var qpWO = EditorQuestPack.OpenWrite(nonClosingStream))
        {
            Assert.Multiple(async () =>
            {
                Assert.That(await qpWO.WriteQuestAsync(questA));
                Assert.That(await qpWO.WriteNodeAsync(questA, stage1));
                Assert.That(await qpWO.WriteNodeAsync(questA, reward1));
                Assert.That(await qpWO.WriteMetadataAsync(questA.Tag, JsonSerializer.Serialize(questAMetadata)));

                Assert.That(await qpWO.WriteQuestAsync(questB));
                Assert.That(await qpWO.WriteNodeAsync(questB, stage1));
                Assert.That(await qpWO.WriteNodeAsync(questB, reward2));
                Assert.That(await qpWO.WriteNodeAsync(questB, stage2));
                Assert.That(await qpWO.WriteMetadataAsync(questB.Tag, JsonSerializer.Serialize(questBMetadata)));
            });
        }

        using var qpRO = EditorQuestPack.OpenRead(nonClosingStream);

        var dQuestA = await qpRO.GetQuestAsync(questA.Tag);
        var dQuestB = await qpRO.GetQuestAsync(questB.Tag);

        var nodesA = (await qpRO.GetNodesAsync(questA.Tag))?.OrderBy(n => n.ID).ToArray();
        var nodesB = (await qpRO.GetNodesAsync(questB.Tag))?.OrderBy(n => n.ID).ToArray();

        var metadataA = await qpRO.GetMetadataAsync<MockMetadata>(questA.Tag);
        var metadataB = await qpRO.GetMetadataAsync<MockMetadata>(questB.Tag);

        Assert.Multiple(() =>
        {
            Assert.That(dQuestA, Is.Not.Null);
            Assert.That(dQuestB, Is.Not.Null);
            Assert.That(nodesA, Is.Not.Null);
            Assert.That(nodesB, Is.Not.Null);
            Assert.That(metadataA, Is.Not.Null);
            Assert.That(metadataB, Is.Not.Null);

            Assert.That(dQuestA!.Tag == questA.Tag && dQuestA.Name == questA.Name);
            Assert.That(dQuestB!.Tag == questB.Tag && dQuestB.Name == questB.Name);

            Assert.That(nodesA, Has.Length.EqualTo(2));
            Assert.That(nodesB, Has.Length.EqualTo(3));

            Assert.That(nodesA![0], Is.TypeOf<StageNode>());
            Assert.That(nodesA[1], Is.TypeOf<RewardNode>());

            Assert.That(nodesB![0], Is.TypeOf<StageNode>());
            Assert.That(nodesB[1], Is.TypeOf<StageNode>());
            Assert.That(nodesB[2], Is.TypeOf<RewardNode>());

            string str = "NodesA: ";
            foreach(var node in nodesA)
            {
                str += $"\n {node.ID} " + node.GetType().Name;
            }
            str += "\nmetadata: " + metadataA!.SomeString;
            str += "\nNodesB: ";
            foreach (var node in nodesB)
            {
                str += $"\n {node.ID} " + node.GetType().Name;
            }
            str += "\nmetadata: " + metadataB!.SomeString;

            TestContext.Out.WriteLine(str);

            Assert.That((nodesA[0] as StageNode)!.JournalEntry, Is.EqualTo(stage1.JournalEntry));
            Assert.That((nodesA[1] as RewardNode)!.Gold, Is.EqualTo(reward1.Gold));

            Assert.That((nodesB[0] as StageNode)!.JournalEntry, Is.EqualTo(stage1.JournalEntry));
            Assert.That((nodesB[1] as StageNode)!.JournalEntry, Is.EqualTo(stage2.JournalEntry));
            Assert.That((nodesB[2] as RewardNode)!.Gold, Is.EqualTo(reward2.Gold));
            Assert.That(metadataA.SomeString, Is.EqualTo(questAMetadata.SomeString));
            Assert.That(metadataB.SomeString, Is.EqualTo(questBMetadata.SomeString));
        });
    }

    [Test(ExpectedResult = true)]
    public async Task<bool> EditorPack_CancelBeforeWrite_DoesNotCorruptArchive()
    {
        var cts = new CancellationTokenSource();
        var stream = new MemoryStream();
        var nonClosingSteam = new NonClosingStream(stream);

        cts.Cancel();

        try
        {
            using var qpWO = EditorQuestPack.OpenWrite(nonClosingSteam, globalToken: cts.Token);
            await qpWO.WriteQuestAsync(new Quest { Tag = "a", Name = "ABC" });
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        cts.Dispose();

        // Now verify the archive is still readable
        stream.Position = 0;

        try
        {
            using var qpRO = EditorQuestPack.OpenRead(stream);
            return true; // archive is not corrupted
        }
        catch
        {
            return false; // archive is corrupted
        }

    }

    [Test]
    public void EditorPack_DisposeDuringWork_DoesNotThrowAndCompleteWork()
    {
        var stream = new MemoryStream();
        var nonClosingStream = new NonClosingStream(stream);

        var qpWO = EditorQuestPack.OpenWrite(nonClosingStream, globalToken:_cts!.Token);

        var quest = new Quest() { Tag = "a", Name = "b" };

        var task = qpWO.WriteQuestAsync(quest);
        qpWO.Dispose();
        Assert.DoesNotThrowAsync(async () => await task);

        using var qpRO = EditorQuestPack.OpenRead(stream);

        Assert.ThatAsync<bool>(async () =>
        {
            var quests = await qpRO.GetQuestsAsync();
            if (quests == null || quests.Length != 1) return false;
            return quests[0].Tag == "a" && quests[0].Name == "b";
        }, Is.True);
    }
}