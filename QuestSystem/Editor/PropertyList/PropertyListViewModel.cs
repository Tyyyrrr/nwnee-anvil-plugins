using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using QuestEditor.PropertyEntry;
using QuestEditor.Shared;

namespace QuestEditor.PropertyList;
public sealed class PropertyListViewModel(object obj) : ViewModelBase
{
    public T? GetT<T>() where T : class
    {
        if(Entries.Count == 0) return default;

        var f = Entries.First();
        return f.GetModel() as T;
    }
    
    public ObservableCollection<PropertyEntryViewModel> Entries { get; } = new ObservableCollection<PropertyEntryViewModel>(
            obj.GetType()
               .GetProperties(
                   BindingFlags.Instance |
                   BindingFlags.Public |
                   BindingFlags.FlattenHierarchy)
               .Where(p => 
                p.CanRead && 
                (p.PropertyType == typeof(string)
                    || !p.PropertyType.IsClass))
               .Select(i => new PropertyEntryViewModel(obj, i))
        );

    public string? Header
    {
        get => _header;
        set
        {
            if (!Equals(_header, value))
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }
    }
    private string? _header;
}