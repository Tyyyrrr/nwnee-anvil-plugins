using QuestEditor.Graph;
using QuestEditor.Objectives;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuestEditor.Shared;

namespace QuestEditor.Nodes
{
    public class NodeControl : ContentControl 
    {
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var graph = this.FindParent<GraphControl>();
            graph?.HandleNodeMouseDown(this);
            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        static NodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeControl), new FrameworkPropertyMetadata(typeof(NodeControl)));
        }

        public IReadOnlyList<ConnectionSocketControl> GetSockets()
        {
            return this.FindChildren<ConnectionSocketControl>();
        }
    }

    public class StageNodeControl : NodeControl
    {

        public static readonly DependencyProperty ObjectivesListProperty = DependencyProperty.Register(
            "ObjectivesList",
            typeof(ObjectiveVM),
            typeof(StageNodeControl),
            new PropertyMetadata(null));

        public ObservableCollection<ObjectiveVM> ObjectivesList
        {
            get => (ObservableCollection<ObjectiveVM>)GetValue(ObjectivesListProperty);
            set => SetValue(ObjectivesListProperty, value);
        }

        static StageNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StageNodeControl), new FrameworkPropertyMetadata(typeof(StageNodeControl)));
        }
    }

    public class RewardNodeControl : NodeControl
    {
        static RewardNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RewardNodeControl), new FrameworkPropertyMetadata(typeof(RewardNodeControl)));
        }
    }
}
