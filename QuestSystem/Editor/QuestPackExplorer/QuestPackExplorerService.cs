using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestEditor.QuestCanvas;
using QuestSystem;

namespace QuestEditor.QuestPackExplorer;


public sealed class QuestPackExplorerService
{
    public string? PackName {get; private set;} = null;

    private List<string> _questTags = [];
    public IReadOnlyList<string> QuestTags => _questTags;

    public string? SelectedQuestTag => Canvas?.Quest.Tag;
    public QuestCanvasModel? Canvas { get; private set; } = null;



    private string? _temporaryQuestPackFile = null;
    private string? _originalQuestPackFile = null;

    private QuestPack? _questPack = null;

    public bool EmptyState => 
        _questPack == null ||
        string.IsNullOrEmpty(_originalQuestPackFile) ||
        string.IsNullOrEmpty(_temporaryQuestPackFile);


    internal void Clear()
    {
        _questPack?.Dispose();
        _questPack = null;

        if(_temporaryQuestPackFile != null && File.Exists(_temporaryQuestPackFile))
            File.Delete(_temporaryQuestPackFile);
            
        _temporaryQuestPackFile = null;
        _originalQuestPackFile = null;

        Canvas = null;
        _questTags = [];
    }

    void OpenTmpPack(string originalFileName)
    {
        Clear();

        _temporaryQuestPackFile = originalFileName+".tmp";

        _originalQuestPackFile = originalFileName;

        File.Copy(_originalQuestPackFile,_temporaryQuestPackFile,true);

        _questPack = QuestPack.OpenRead(_temporaryQuestPackFile);
        
        PackName = Path.GetFileNameWithoutExtension(_originalQuestPackFile);

        RefreshQuestTags();
    }

    public void CreatePackFile(string? filePath)
    {
        if(string.IsNullOrEmpty(filePath))
            return;

        if (File.Exists(filePath)) 
            File.Delete(filePath);

        QuestPack.OpenWrite(filePath).Dispose(); // create empty zipArchive
        
        OpenTmpPack(filePath);
    }

    public void SelectPackFile(string? filePath)
    {
        if(string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Clear();
            return;
        }
            
        OpenTmpPack(filePath);
    }

    public void SaveCurrentPack()
    {
        if (EmptyState)
        {
            Console.WriteLine("Nothing to save.");
            return;
        }

        _questPack!.Dispose();
        _questPack = null;

        File.Copy(_temporaryQuestPackFile!, _originalQuestPackFile!, true);

        Console.WriteLine("Saved quest pack to " + _originalQuestPackFile);

        OpenTmpPack(_originalQuestPackFile!);
    }

    public void SaveCurrentPackAs(string? fileName)
    {
        if (EmptyState || string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("Nothing to save.");
            return;
        }

        _questPack!.Dispose();
        _questPack = null;

        File.Copy(_temporaryQuestPackFile!, fileName, true);

        Console.WriteLine("Saved quest pack to " + fileName);

        OpenTmpPack(fileName);
    }


    public bool AddQuest(string questTag)
    {
        if(EmptyState || QuestTags.Contains(questTag))
            return false;

        var json = Quest.Serialize(new());

        _questPack!.Dispose();
        var qp = QuestPack.OpenWrite(_temporaryQuestPackFile!);

        var entry = qp.CreateEntry(questTag+"/q");

        using (var sw = new StreamWriter(entry.Open())) { sw.Write(json); }

        qp.Dispose();
        _questPack = QuestPack.OpenRead(_temporaryQuestPackFile!);

        _questTags.Add(questTag);

        return true;
    }

    public bool RemoveQuest(string? questTag)
    {
        if(EmptyState || string.IsNullOrEmpty(questTag) || !QuestTags.Contains(questTag))
            return false;

        _questPack!.Dispose();
        var qp = QuestPack.OpenWrite(_temporaryQuestPackFile!);

        var entriesToDelete = qp.Entries.Where(e=>e.FullName.StartsWith(questTag, StringComparison.OrdinalIgnoreCase)).ToList();
        
        foreach(var entry in entriesToDelete) entry.Delete();

        qp.Dispose();
        _questPack = QuestPack.OpenRead(_temporaryQuestPackFile!);

        _questTags.Remove(questTag);
        return true;
    }

    public bool SelectQuest(string? questTag)
    {
        if(EmptyState || SelectedQuestTag==questTag || string.IsNullOrEmpty(questTag) || !QuestTags.Contains(questTag))
            return false;

        var entries = _questPack!.Entries.Where(e=>e.FullName.StartsWith(questTag, StringComparison.OrdinalIgnoreCase));

        Quest? quest = null;
        List<QuestStage> stages = [];
        bool succeeded = true;
        foreach(var entry in entries)
        {
            string json;
            using(var sr = new StreamReader(entry.Open())) { json = sr.ReadToEnd(); }

            if (entry.FullName.EndsWith("/q"))
            {
                quest = Quest.Deserialize(json);

                if(quest == null)
                {
                    Console.WriteLine($"Failed to deserialize quest \'{questTag}\'");
                    succeeded = false;
                    break;
                }

                continue;
            }

            var stage = QuestStage.Deserialize(json);
            if(stage == null)
            {
                Console.WriteLine("Failed to deserialize stage no."+entry.FullName[^1]);
                succeeded = false;
                break;
            }
            stages.Add(stage);
        }

        if(succeeded)
        {
            Canvas = new(quest!, stages);
            CanvasChanged?.Invoke();
        }

        return succeeded;
    }

    public event Action? CanvasChanged;

    void RefreshQuestTags()
    {
        if(_questPack == null)
        {
            Clear();
            return;
        }

        _questTags = [.. _questPack.Entries
            .Select(e => e.FullName.Split('/')[0])
            .Distinct()
            .OrderBy(x => x)];
    }
}