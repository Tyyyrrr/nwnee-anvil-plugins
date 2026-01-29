using QuestEditor.Graph;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuestEditor.Nodes
{
    public class NodeControl : ContentControl 
    {
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var graph = FindParent<GraphControl>(this);
            graph?.BeginDrag((NodeVM)DataContext);
            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }


        public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }


        static NodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeControl), new FrameworkPropertyMetadata(typeof(NodeControl)));
        }
    }
}
