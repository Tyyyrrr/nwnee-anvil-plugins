using System.Collections.Generic;
using System.Windows;
using QuestEditor.QuestCanvas;

namespace QuestEditor.QuestPackExplorer
{
    public class QuestEditorMetadata
    {
        public Dictionary<int, Point> NodePositions {get;set;} = [];
        public QuestEditorMetadata(QuestCanvasViewModel canvasVM)
        {
            foreach(var node in canvasVM.StageNodes)
            {
                NodePositions.Add(node.StageID, new(node.X,node.Y));
            }
        }

        public QuestEditorMetadata(){}
    }
}