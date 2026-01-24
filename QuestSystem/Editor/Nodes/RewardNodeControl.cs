using System.Windows;

namespace QuestEditor.Nodes
{
    public class RewardNodeControl : NodeControl
    {
        static RewardNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RewardNodeControl), new FrameworkPropertyMetadata(typeof(RewardNodeControl)));
        }
    }
}
