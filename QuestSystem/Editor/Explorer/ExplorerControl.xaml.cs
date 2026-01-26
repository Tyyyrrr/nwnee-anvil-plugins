using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuestEditor.Explorer
{
    public partial class ExplorerControl : UserControl
    {
        private static readonly DependencyProperty MouseLeftButtonDownCommandProperty = DependencyProperty.Register(
            "MouseLeftButtonDownCommand",
            typeof(ICommand),
            typeof(ExplorerControl), new PropertyMetadata(null));

        private ICommand MouseLeftButtonDownCommand
        {
            get => (ICommand)GetValue(MouseLeftButtonDownCommandProperty);
            set => SetValue(MouseLeftButtonDownCommandProperty, value);
        }

        public ExplorerControl()
        {
            InitializeComponent();
        }

        private void SelectableTreeViewItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseLeftButtonDownCommand.Execute(((SelectableTreeViewItem)sender).DataContext);
        }
    }
}
