using System;
using System.Windows.Controls;

namespace QuestEditor.QuestPackExplorer
{
    public partial class QuestPackExplorerView : UserControl
    {    
        public QuestPackExplorerViewModel ViewModel
        {
            get => (QuestPackExplorerViewModel)DataContext;
            set => DataContext = value;
        }

        public QuestPackExplorerView()
        {
            InitializeComponent();
        }
    }
}
