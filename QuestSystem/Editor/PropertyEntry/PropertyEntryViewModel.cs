using System;
using System.Globalization;
using System.Reflection;
using QuestEditor.Shared;

namespace QuestEditor.PropertyEntry;

public sealed class PropertyEntryViewModel(object model, PropertyInfo info) : ViewModelBase
{
    public object GetModel() => _model;
    private readonly object _model = model;
    private readonly PropertyInfo _info = info;

    public string PropertyName => _info.Name;
    public Type PropertyType => _info.PropertyType;

    public bool IsEnum => PropertyType.IsEnum;
    public bool IsBoolean => PropertyType == typeof(bool);
    public bool IsNumeric => PropertyType.IsPrimitive && PropertyType != typeof(bool) && PropertyType != typeof(char);
    public bool IsString => PropertyType == typeof(string);

    public bool CanWrite => _info.CanWrite;

    public Array? EnumValues => IsEnum ? Enum.GetValues(PropertyType) : null;

    private object? _value = info.GetValue(model);
    public object? PropertyValue
    {
        get => _value;
        set
        {
            if (Equals(_value, value)) return;

            object? converted = value;

            var targetType = PropertyType;
            if (targetType.IsEnum)
            {
                converted = value;
            }
            else if (value is string s && targetType != typeof(string))
            {
                if (targetType.IsEnum)
                    converted = targetType.Equals(value.GetType()) ? value : Enum.Parse(targetType, s);
                else
                    converted = Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
            }

            _value = converted;
            _info.SetValue(_model, converted);
            OnPropertyChanged(nameof(PropertyValue));
        }
    }

    
}