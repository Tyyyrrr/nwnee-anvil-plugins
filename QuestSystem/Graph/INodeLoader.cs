namespace QuestSystem.Graph
{
    internal interface INodeLoader
    {
        internal INode? LoadNode(Quest quest, int nodeId);
    }
}