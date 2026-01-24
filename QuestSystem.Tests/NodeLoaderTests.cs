using QuestSystem.Graph;
using QuestSystem.Nodes;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Tests
{
    [TestFixture(TestOf = typeof(NodeLoader))]
    internal class NodeLoaderTests
    {
        [Test]
        public async Task NodeLoader_ValidPack_LoadsExpectedNode()
        {
            var nodeLoader = new NodeLoader();

            var stream = new MemoryStream();
            EditorQuestPackTests.NonClosingStream ncs = new(stream);

            using (var eqpWO = EditorQuestPack.OpenWrite(ncs))
            {
                var quest = new Quest() { Tag = "test" };
                var node = new StageNode() { ID = 0, JournalEntry = "test entry" };

                await eqpWO.WriteQuestAsync(quest);
                await eqpWO.WriteNodeAsync(quest, node);
            }

            var rqpRO = new RuntimeQuestPack(stream);

            var rQuest = rqpRO.GetQuest("test")!;
            rQuest.Pack = rqpRO;
            var rNode = ((INodeLoader)nodeLoader).LoadNode(rQuest, 0);
            Assert.Multiple(() =>
            {
                Assert.That(rNode, Is.Not.Null.And.TypeOf<StageNodeWrapper>());
                Assert.That((rNode as StageNodeWrapper)!.Node.JournalEntry, Is.EqualTo("test entry"));
            });
        }
    }
}
