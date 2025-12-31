using QuestSystem;

namespace QuestEditor.Shared;

public interface IDialogService
{
    string? ShowOpenFileDialog();
    string? ShowSaveFileDialog(string defaultFileName);
}

public sealed class QuestPackDialogService : IDialogService
{
    public string? ShowOpenFileDialog()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = QuestPack.FileExtension,
            Filter = $"Quest packs ({QuestPack.FileExtension})|*{QuestPack.FileExtension}",
            AddToRecent=true
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveFileDialog(string defaultFileName)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = QuestPack.FileExtension,
            Filter = $"Quest packs ({QuestPack.FileExtension})|*{QuestPack.FileExtension}",
            AddToRecent = true
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
