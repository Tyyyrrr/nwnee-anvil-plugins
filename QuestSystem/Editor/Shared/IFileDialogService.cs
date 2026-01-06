
namespace QuestEditor.Shared;

public interface IFileDialogService
{
    string? ShowOpenFileDialog();
    string? ShowSaveFileDialog(string defaultFileName);
}
