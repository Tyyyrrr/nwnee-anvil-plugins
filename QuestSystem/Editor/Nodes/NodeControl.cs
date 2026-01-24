using System.Windows;
using System.Windows.Controls;

namespace QuestEditor.Nodes
{
    public class NodeControl : Control
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), 
            typeof(object),
            typeof(NodeControl), 
            new PropertyMetadata(null)); 

        public object Content 
        { 
            get => GetValue(ContentProperty); 
            set => SetValue(ContentProperty, value); 
        }

        static NodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeControl), new FrameworkPropertyMetadata(typeof(NodeControl)));
        }
    }
}
