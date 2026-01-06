using System.Windows;
using System.Windows.Controls;

namespace QuestEditor.PropertyEntry;
public sealed class PropertyEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? StringTemplate { get; set; }
    public DataTemplate? NumericTemplate { get; set; }
    public DataTemplate? BooleanTemplate { get; set; }
    public DataTemplate? EnumTemplate { get; set; }
    public DataTemplate? ObjectTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is not PropertyEntryViewModel vm)
            return base.SelectTemplate(item, container);

        if (vm.IsEnum) return EnumTemplate!;
        if (vm.IsBoolean) return BooleanTemplate!;
        if (vm.IsNumeric) return NumericTemplate!;
        if (vm.IsString) return StringTemplate!;
        return ObjectTemplate!;
    }
}
