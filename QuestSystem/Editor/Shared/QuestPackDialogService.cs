using QuestSystem;

namespace QuestEditor.Shared;

public sealed class QuestPackDialogService : IFileDialogService
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
