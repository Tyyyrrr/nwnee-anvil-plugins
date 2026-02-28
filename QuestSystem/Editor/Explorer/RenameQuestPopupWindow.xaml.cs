using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace QuestEditor.Explorer
{
    public partial class RenameQuestPopupWindow : Window
    {
        public RenameQuestPopupWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => RenameBox.Focus();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
