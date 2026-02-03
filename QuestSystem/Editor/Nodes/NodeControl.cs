using QuestEditor.Graph;
using QuestEditor.Objectives;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuestEditor.Shared;
using System.Windows.Media;
using System.Diagnostics;

namespace QuestEditor.Nodes
{
    public class NodeControl : ContentControl 
    {
        public static readonly DependencyProperty InputColorBrushProperty = DependencyProperty.Register(
            "InputColorBrush",
            typeof(SolidColorBrush),
            typeof(NodeControl), new PropertyMetadata(Brushes.Black));

        public SolidColorBrush InputColorBrush
        {
            get => (SolidColorBrush)GetValue(InputColorBrushProperty);
            set => SetValue(InputColorBrushProperty, value);
        }

        public static readonly DependencyProperty OutputColorBrushProperty = DependencyProperty.Register(
            "OutputColorBrush",
            typeof(SolidColorBrush),
            typeof(NodeControl), new PropertyMetadata(Brushes.Black));

        public SolidColorBrush OutputColorBrush
        {
            get => (SolidColorBrush)GetValue(OutputColorBrushProperty);
            set => SetValue(OutputColorBrushProperty, value);
        }

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
    public class VisibilityNodeControl : NodeControl
    {
        static VisibilityNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VisibilityNodeControl), new FrameworkPropertyMetadata(typeof(VisibilityNodeControl)));
        }
    }

    public class RandomizerNodeControl : NodeControl
    {
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            (DataContext as RandomizerNodeVM)?.TryPushUndoableChanges();
            base.OnLostKeyboardFocus(e);
        }

        static RandomizerNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RandomizerNodeControl), new FrameworkPropertyMetadata(typeof(RandomizerNodeControl)));
        }
    }

    public class CooldownNodeControl : NodeControl
    {
        static CooldownNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CooldownNodeControl), new FrameworkPropertyMetadata(typeof(CooldownNodeControl)));
        }
    }

    public class ConditionNodeControl : NodeControl
    {
        static ConditionNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConditionNodeControl), new FrameworkPropertyMetadata(typeof(ConditionNodeControl)));
        }
    }

    public class UnknownNodeControl : NodeControl
    {
        static UnknownNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UnknownNodeControl), new FrameworkPropertyMetadata(typeof(UnknownNodeControl)));
        }
    }
}
