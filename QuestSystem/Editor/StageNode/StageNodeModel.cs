using System;

namespace QuestEditor.StageNode
{
    public sealed class StageNodeModel
    {
        public StageNodeModel(string json)
        {
            Console.WriteLine("Stage JSON:\n" + json);
        }
    }
}