
using System.Windows;

namespace QuestEditor.Shared;

public interface IMessageBoxService
{
    int Show(string text, string caption, MessageBoxButton button);
}
