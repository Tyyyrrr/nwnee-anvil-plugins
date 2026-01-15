namespace QuestSystem.Graph
{
    internal interface INodeLoader
    {
        internal INode? LoadNode(string questTag, int nodeId);
    }
}