using System.Windows;

namespace QuestEditor.Shared;

public sealed class MessageBoxService : IMessageBoxService
{
    public int Show(string text, string caption, MessageBoxButton button) => 
        MessageBox.Show(text,caption,button) switch
        {
            MessageBoxResult.OK or MessageBoxResult.Yes => 1,
            MessageBoxResult.No => -1,
            _ => 0
        };
}
