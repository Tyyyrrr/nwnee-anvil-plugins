using System.Windows;
using System.Windows.Controls;

namespace QuestEditor.Objectives
{

    public class ObjectiveControl : ContentControl
    {
        static ObjectiveControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveControl), new FrameworkPropertyMetadata(typeof(ObjectiveControl)));
        }
    }
}
