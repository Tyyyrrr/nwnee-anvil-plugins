using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestSystem;

namespace QuestEditor.QuestPackExplorer
{
    public static class QuestPackExplorerModel
    {
        private static string? _temporaryQuestPackFile = null;
        private static string? _originalQuestPackFile = null;

        private static QuestPack? _questPackWriteOnly = null;
        private static QuestPack? _questPackReadOnly = null;

        private static bool EmptyState => 
            _questPackReadOnly == null || 
            _questPackWriteOnly == null ||
            string.IsNullOrEmpty(_originalQuestPackFile) ||
            string.IsNullOrEmpty(_temporaryQuestPackFile);

        public static string? SelectedPackFileName => Path.GetFileNameWithoutExtension(_originalQuestPackFile);

        private static Quest? _selectedQuest;
        public static IReadOnlyList<string> QuestTags {get;private set;} = [];
        public static string? SelectedQuestTag => _selectedQuest?.Tag;

        public static void Clear()
        {
            _questPackReadOnly?.Dispose();
            _questPackReadOnly = null;

            _questPackWriteOnly?.Dispose();
            _questPackWriteOnly = null;

            if(_temporaryQuestPackFile != null && File.Exists(_temporaryQuestPackFile))
                File.Delete(_temporaryQuestPackFile);
                
            _temporaryQuestPackFile = null;
            _originalQuestPackFile = null;

            _selectedQuest = null;
            QuestTags = [];
        }

        static void OpenTmpPack(string originalFileName)
        {
            Clear();

            var tmpPath = originalFileName+".tmp";

            if(File.Exists(tmpPath)) File.Delete(tmpPath);

            _temporaryQuestPackFile = tmpPath;
            _originalQuestPackFile = originalFileName;

            _questPackReadOnly = QuestPack.OpenRead(_originalQuestPackFile);
            _questPackWriteOnly = QuestPack.OpenWrite(_temporaryQuestPackFile);
            
            RefreshQuestTags();
        }

        public static void CreatePackFile()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "NewQuestPack",
                DefaultExt = QuestPack.FileExtension,
                Filter = $"Quest packs ({QuestPack.FileExtension})|*{QuestPack.FileExtension}",
                AddToRecent=true
            };

            if(!dlg.ShowDialog() ?? true) return;

            if (File.Exists(dlg.FileName)) 
                File.Delete(dlg.FileName);

            QuestPack.OpenWrite(dlg.FileName).Dispose();
            
            OpenTmpPack(dlg.FileName);
        }

        public static void SelectPackFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = QuestPack.FileExtension,
                Filter = $"Quest packs ({QuestPack.FileExtension})|*{QuestPack.FileExtension}",
                AddToRecent=true
            };

            if(!dlg.ShowDialog() ?? true) return;

            if(!File.Exists(dlg.FileName))
                QuestPack.OpenWrite(dlg.FileName).Dispose();
                
            OpenTmpPack(dlg.FileName);
        }

        public static void SaveCurrentPack()
        {
            if (EmptyState)
            {
                Console.WriteLine("Nothing to save.");
                return;
            }

            _questPackReadOnly!.Dispose();
            _questPackReadOnly = null;
            _questPackWriteOnly!.Dispose();
            _questPackWriteOnly = null;

            File.Copy(_temporaryQuestPackFile!, _originalQuestPackFile!);

            Console.WriteLine("Saved quest pack to " + _originalQuestPackFile);

            OpenTmpPack(_originalQuestPackFile!);
        }

        public static void SaveCurrentPackAs()
        {
            if (EmptyState)
            {
                Console.WriteLine("Nothing to save.");
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = _originalQuestPackFile ?? "NewQuestPack",
                DefaultExt = QuestPack.FileExtension,
                Filter = $"Quest packs ({QuestPack.FileExtension})|*{QuestPack.FileExtension}",
                AddToRecent=true
            };

            if(!dlg.ShowDialog() ?? true) return;

            _questPackReadOnly!.Dispose();
            _questPackReadOnly = null;
            _questPackWriteOnly!.Dispose();
            _questPackWriteOnly = null;

            File.Copy(_temporaryQuestPackFile!, dlg.FileName, true);

            Console.WriteLine("Saved quest pack to " + dlg.FileName);

            OpenTmpPack(dlg.FileName);
        }


        public static bool AddQuest(string questTag)
        {
            
        }

        public static bool RemoveQuest(string questTag)
        {

        }

        public static bool SelectQuest(string questTag)
        {
            
        }

        static void RefreshQuestTags()
        {
            if(_questPackWriteOnly == null)
            {
                Clear();
                return;
            }

            QuestTags = [.._questPackWriteOnly.Entries.Select(e=>e.FullName.Split(Path.AltDirectorySeparatorChar)[0]).Order().ToHashSet()];
        }

    }    
}