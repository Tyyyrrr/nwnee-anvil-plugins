namespace QuestEditor.Shared
{
    public readonly record struct StringIntListItem(string StringValue, int IntValue);
    public readonly record struct StringFloatListItem(string StringValue, float FloatValue);

    public readonly record struct StringBoolListItem(string StringValue, bool BoolValue);
    public sealed class StringBoolListItemVM : ViewModelBase
    {
        public string StringValue { get; init; } = string.Empty;
        public bool BoolValue
        {
            get => _boolValue;
            set => SetProperty(ref _boolValue, value);
        }
        private bool _boolValue;
    }

    public sealed class  FloatIntListItemVM : ViewModelBase
    {
        public float FloatValue
        {
            get => _floatValue;
            set => SetProperty(ref _floatValue, value);
        } float _floatValue;

        public int IntValue
        {
            get => _intValue;
            set => SetProperty(ref _intValue, value);
        } int _intValue;
    }
}
