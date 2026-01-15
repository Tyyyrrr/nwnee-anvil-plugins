namespace QuestSystem.Graph
{
    internal interface INodeLoader
    {
        public INode? LoadNode(string questTag, int nodeId);
    }
}