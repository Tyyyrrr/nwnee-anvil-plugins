using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace QuestEditor.Explorer
{
    public class SelectableTreeViewItem : TreeViewItem
    {
        public static readonly DependencyProperty ItemSelectedProperty = DependencyProperty.Register(
            "ItemSelected",
            typeof(bool),
            typeof(SelectableTreeViewItem),
            new PropertyMetadata(false));
        public bool ItemSelected
        { 
            get => (bool)GetValue(ItemSelectedProperty);
            set => SetValue(ItemSelectedProperty, value);
        }

        public static readonly DependencyProperty ClickedCommandProperty = DependencyProperty.Register(
            "ClickedCommand",
            typeof(ICommand),
            typeof(SelectableTreeViewItem),
            new PropertyMetadata(null));

        public ICommand ClickedCommand
        {
            get => (ICommand)GetValue(ClickedCommandProperty);
            set => SetValue(ClickedCommandProperty, value);
        }


        static SelectableTreeViewItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectableTreeViewItem), new FrameworkPropertyMetadata(typeof(SelectableTreeViewItem)));
        
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            ClickedCommand.Execute(null);
            base.OnMouseLeftButtonDown(e);
        }

    }
}
