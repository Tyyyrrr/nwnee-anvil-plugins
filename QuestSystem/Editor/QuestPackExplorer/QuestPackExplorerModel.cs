using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestSystem;

namespace QuestEditor.QuestPackExplorer
{
    public static class QuestPackExplorerModel
    {
        private static string? _tmpQuestPackFile = null;
        private static string? _originalQuestPackFile = null;
        private static QuestPack? _selectedQuestPack = null;
        private static Quest? _currentQuest;
        public static IReadOnlyList<string> QuestTags {get;private set;} = [];

        public static string? SelectedPackFileName => Path.GetFileNameWithoutExtension(_originalQuestPackFile);
        public static string? CurrentQuestTag => _currentQuest?.Tag;

        public static void Clear()
        {
            _selectedQuestPack?.Dispose();
            _selectedQuestPack = null;
            if(_tmpQuestPackFile != null && File.Exists(_tmpQuestPackFile))
            {
                try
                {
                    File.Delete(_tmpQuestPackFile);
                }
                catch (Exception)//(Exception ex)
                {
                    Console.WriteLine("Failed to delete temporary quest pack file.");
                }
            }
            _tmpQuestPackFile = null;
            _originalQuestPackFile = null;
            QuestTags = [];
        }

        static void OpenTmpPack(string originalFileName, bool createNew)
        {

            var tmpPath = originalFileName+".tmp";

            if(createNew)
            {
                if(File.Exists(originalFileName))
                {
                    Console.WriteLine("File already exists!");
                    Clear();
                    return;
                }
                Console.WriteLine("Creating new temporary quest pack file at " + tmpPath);
            }


            if(File.Exists(tmpPath))
            {
                Console.WriteLine("Temporary quest pack file already exists. Deleting...");

                try
                {
                    File.Delete(tmpPath);
                }
                catch (Exception)//(Exception ex)
                {
                    // log ex.Message
                    Clear();
                    return;
                }
            }

            if(!createNew)
            {
                Console.WriteLine("Creating temporary quest pack file from existing at " + tmpPath);
                File.Copy(originalFileName, tmpPath);
            }

            if(_tmpQuestPackFile != null) Clear();

            _tmpQuestPackFile = tmpPath;
            _originalQuestPackFile = originalFileName;

            _selectedQuestPack = QuestPack.OpenWrite(_tmpQuestPackFile);
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

            if(File.Exists(dlg.FileName)) 
                OpenTmpPack(dlg.FileName, false);
            else 
                OpenTmpPack(dlg.FileName, true);
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
            {
                Console.WriteLine("Selected file does not exist.");
                OpenTmpPack(dlg.FileName, true);
            }
            else OpenTmpPack(dlg.FileName, false);
        }

        public static void SaveCurrentPack()
        {
            if(_selectedQuestPack == null || string.IsNullOrEmpty(_tmpQuestPackFile) || string.IsNullOrEmpty(_originalQuestPackFile))
            {
                Console.WriteLine("No quest pack to save.");
                return;
            }

            try
            {
                File.Copy(_tmpQuestPackFile, _originalQuestPackFile, true);
                Console.WriteLine("Saved quest pack to " + _originalQuestPackFile);
            }
            catch(Exception)
            {
                Console.WriteLine("Failed to save quest pack to " + _originalQuestPackFile);
            }
        }

        public static void SaveCurrentPackAs()
        {
            if(_selectedQuestPack == null || string.IsNullOrEmpty(_tmpQuestPackFile))
            {
                Console.WriteLine("No quest pack to save.");
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

            try
            {
                File.Copy(_tmpQuestPackFile, dlg.FileName, true);
                Console.WriteLine("Saved quest pack to " + dlg.FileName);
            }
            catch(Exception)
            {
                Console.WriteLine("Failed to save quest pack to " + dlg.FileName);
            }
        }


        public static async ValueTask<bool> AddQuest(string questTag)
        {
            if(string.IsNullOrEmpty(questTag) || _selectedQuestPack == null || QuestTags.Contains(questTag)) 
                return false;

            _currentQuest = new Quest { Tag = questTag };

            await _selectedQuestPack.AddQuestAsync(_currentQuest);

            return true;
        }

        public static bool RemoveQuest(string questTag)
        {
            if(string.IsNullOrEmpty(questTag) || _selectedQuestPack == null || !QuestTags.Contains(questTag))
                return false;

            if(_currentQuest != null && _currentQuest.Tag == questTag)
            {
                _currentQuest = null;
            }
                
            QuestTags = [.. QuestTags.Where(qt => qt != questTag)];

            foreach(var entry in _selectedQuestPack.Entries.Where(e => e.FullName.StartsWith(questTag)))
            {
                entry.Delete();
            }

            return true;
        }
    }    
}