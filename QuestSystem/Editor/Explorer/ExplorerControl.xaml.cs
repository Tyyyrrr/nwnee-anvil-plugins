using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuestEditor.Explorer
{
    public partial class ExplorerControl : UserControl
    {
        public static readonly DependencyProperty ClearSelectionCommandProperty = DependencyProperty.Register(
            "ClearSelectionCommand",
            typeof(ICommand),
            typeof(ExplorerControl), new PropertyMetadata(null));

        public ICommand ClearSelectionCommand
        {
            get => (ICommand)GetValue(ClearSelectionCommandProperty);
            set => SetValue(ClearSelectionCommandProperty, value);
        }

        public ExplorerControl()
        {
            InitializeComponent();
        }

        private void Explorer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClearSelectionCommand.Execute(null);
        }

        private void SelectableTreeView_MouseEnter(object sender, MouseEventArgs e)
        {
            Keyboard.Focus(this);
        }
    }
}
