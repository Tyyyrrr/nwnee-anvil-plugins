using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuestEditor.Shared
{
    public abstract class ViewModelBase : INotifyPropertyChanging, INotifyPropertyChanged
    {
        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if(Equals(storage,value)) 
                return false;

            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            storage = value;
            RaisePropertyChanged(propertyName ?? string.Empty);

            return true;
        }

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}