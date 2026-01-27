using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace QuestEditor.Explorer
{
    [TemplatePart(Name = "PART_ItemsHost", Type = typeof(Panel))]
    public class SelectableTreeViewItem : TreeViewItem
    {
        static SelectableTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SelectableTreeViewItem),
                new FrameworkPropertyMetadata(typeof(SelectableTreeViewItem)));

            ItemsPanelProperty.OverrideMetadata(
                typeof(SelectableTreeViewItem),
                new FrameworkPropertyMetadata(
                    new ItemsPanelTemplate(
                        new FrameworkElementFactory(typeof(StackPanel)))));
        }

        protected override DependencyObject GetContainerForItemOverride() => new SelectableTreeViewItem();
        protected override bool IsItemItsOwnContainerOverride(object item) => item is SelectableTreeViewItem;




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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            ClickedCommand.Execute(DataContext);
            base.OnMouseLeftButtonDown(e);
        }
    }
}
