using System.Windows;
using System.Windows.Controls;

namespace QuestEditor.Graph
{
    public sealed class ConnectionControl : Control
    {
        public static readonly DependencyProperty FromToProperty = DependencyProperty.Register(
            "FromTo",
            typeof(FromToPoint),
            typeof(ConnectionControl),
            new FrameworkPropertyMetadata(null));

        public FromToPoint FromTo
        {
            get => (FromToPoint)GetValue(FromToProperty);
            set => SetValue(FromToProperty, value);
        }

        static ConnectionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionControl), new FrameworkPropertyMetadata(typeof(ConnectionControl)));
        }
    }
}
