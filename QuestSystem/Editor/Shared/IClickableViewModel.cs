using System.Windows.Input;

namespace QuestEditor.Shared
{
    public interface IClickableViewModel
    {
        public ICommand ClickedCommand { get; }
    }
}