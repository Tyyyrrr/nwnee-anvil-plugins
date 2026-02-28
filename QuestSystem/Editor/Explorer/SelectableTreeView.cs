using System.Windows.Controls;
using System.Windows;

namespace QuestEditor.Explorer
{
    public class SelectableTreeView : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new SelectableTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is SelectableTreeViewItem;
        }
    }

}