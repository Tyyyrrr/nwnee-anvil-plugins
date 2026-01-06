using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using QuestSystem;

namespace QuestEditor.QuestPackExplorer;


public sealed class QuestPackExplorerService
{
    public string? PackName {get; private set;} = null;

    private List<string> _questTags = [];
    public IReadOnlyList<string> QuestTags => _questTags;   

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

        var json = Quest.Serialize(new(){Tag = questTag});

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
        Console.WriteLine("Selecting quest " + questTag);

        var canvasVM = ((App)Application.Current).CanvasVM;

        if(string.IsNullOrEmpty(questTag))
        {
            canvasVM.SetModel(null);
            return true;
        }

        if(!QuestTags.Contains(questTag)) throw new InvalidOperationException("Tag not in set");

        var entries = _questPack!.Entries.Where(e=>e.FullName.StartsWith(questTag, StringComparison.OrdinalIgnoreCase));

        Quest? quest = null;
        QuestEditorMetadata? metadata = null;
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

            if (entry.FullName.EndsWith("/editor.meta"))
            {
                metadata = JsonSerializer.Deserialize<QuestEditorMetadata>(json);
                Console.WriteLine("Deserialized metadata: " + json);
                continue;
            }

            var stage = QuestStage.Deserialize(json);
            if(stage == null)
            {
                Console.WriteLine("Failed to deserialize stage no."+entry.FullName[^1]);
                succeeded = false;
                break;
            }
            else
            {
                Console.WriteLine("Deserialized stage with " + stage.Objectives.Length + " objectives");
            }
            stages.Add(stage);
        }

        if(succeeded && quest != null)
        {
            Console.WriteLine("Deserialized quest with " + stages.Count + " stages");
            canvasVM.SetModel(new(quest!,stages), metadata);
            Console.WriteLine("Edit mode on");
        }
        else{
            canvasVM.SetModel(null);
            Console.WriteLine("Edit mode off");
        }

        return succeeded;
    }

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

    public void ApplyChanges(string selectedQuestTag)
    {
        Console.WriteLine("Explorer forces applying changes...");
        var qt = selectedQuestTag;
        if(qt == null || !QuestTags.Contains(qt)) return;

        Console.WriteLine("...quest tag found...");

        var canvasVM = ((App)Application.Current).CanvasVM;
        var canvas = canvasVM.ApplyChangesToModel();

        Quest quest = canvas?.Quest ?? new(){Tag=qt};
        QuestStage[] stages;

        if(canvas == null)
        {
            Console.WriteLine("... canvas is null ...");
            stages = [];
        }
        else
        {
            Console.WriteLine("... canvas is not null ...");

            stages = new QuestStage[canvas.Stages.Count];

            var ids = canvas.Stages.Keys.Order().ToArray();

            for(int i =0; i < stages.Length; i++)
            {
                stages[i]=canvas.Stages[ids[i]];
            }

            Console.WriteLine(" ... quest now has " + stages.Length + " stages.");
        }

        _questPack?.Dispose();
        
        _questPack = QuestPack.OpenWrite(_temporaryQuestPackFile!);

        var entriesToDelete = _questPack.Entries.Where(e=>e.FullName.StartsWith(qt)).ToArray();

        foreach(var etd in entriesToDelete) etd.Delete();

        Console.WriteLine("Saving quest " + quest.Tag);
        _ = _questPack.AddQuestAsync(quest).GetAwaiter().GetResult();



        foreach(var stage in stages)
        {
            Console.WriteLine("Saving stage " + stage.ID + " with " + stage.Objectives.Length + " objectives");
            _ = _questPack.SetStageAsync(qt,stage).GetAwaiter().GetResult();
        }

        var metadata = new QuestEditorMetadata(canvasVM);

        var json = JsonSerializer.Serialize(metadata);

        Console.WriteLine("saved metadata: " + metadata.NodePositions.Count);
        string str = "";
        foreach(var kvp in metadata.NodePositions) str += "\n"+kvp.Key+"  " + kvp.Value.X + " | " + kvp.Value.Y;
        Console.WriteLine(str);
        Console.WriteLine("\nJSON:\n"+json);

        var entry = _questPack.CreateEntry($"{qt}/editor.meta");
        
        using(var sw = new StreamWriter(entry.Open()))
        {
            sw.Write(json);
        }

        _questPack.Dispose();

        _questPack = QuestPack.OpenRead(_temporaryQuestPackFile!);
    }
}