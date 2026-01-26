using Microsoft.Win32;
using QuestSystem;

namespace QuestEditor.Shared
{
    internal sealed class FileDialog : IOpenFilesDialog, ICreateFileDialog
    {
        public string GetFileNameFromUser()
        {
            var cfd = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = QuestPack.FileExtension,
                Filter = "Quest packs (.qp) | *.qp",
                FilterIndex = 0,
                AddToRecent = true,
                CreateTestFile = true,
                OverwritePrompt = true,
                ValidateNames = true,
            };

            if (cfd.ShowDialog() ?? false)
                return cfd.FileName;

            return string.Empty;
        }

        public string[] GetFileNamesFromUser()
        {
            var ofd = new OpenFileDialog()
            {
                AddExtension = true,
                DefaultExt = QuestPack.FileExtension,
                Filter = "Quest packs (.qp) | *.qp",
                FilterIndex = 0,
                AddToRecent = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true,
                ValidateNames = true,
                Title = "Select packs"
            };

            if(ofd.ShowDialog() ?? false)
                return ofd.FileNames;

            return [];
        }
    }
}