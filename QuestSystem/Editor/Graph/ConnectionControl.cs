using System.Windows;
using System.Windows.Controls;

namespace QuestEditor.Graph
{
    public class ConnectionControl : Control
    {
        static ConnectionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionControl), new FrameworkPropertyMetadata(typeof(ConnectionControl)));
        }
    }
}
