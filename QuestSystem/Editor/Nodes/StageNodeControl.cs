using System.Windows;

namespace QuestEditor.Nodes
{
    public class StageNodeControl : NodeControl
    {
        static StageNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StageNodeControl), new FrameworkPropertyMetadata(typeof(StageNodeControl)));
        }
    }
}
