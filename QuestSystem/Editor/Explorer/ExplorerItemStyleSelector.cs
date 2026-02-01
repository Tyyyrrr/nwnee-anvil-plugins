using QuestEditor.Nodes;
using System.Windows.Controls;
using System.Windows;

namespace QuestEditor.Explorer
{
    public class ExplorerItemStyleSelector : StyleSelector
    {
        public Style? QuestPackStyle { get; set; }
        public Style? QuestStyle { get; set; }
        public Style? NodeStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            return item switch
            {
                QuestPackVM => QuestPackStyle!,
                QuestVM => QuestStyle!,
                NodeVM => NodeStyle!,
                _ => base.SelectStyle(item, container)
            };
        }
    }

}
