using System;
using System.Windows;

namespace QuestEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern bool AllocConsole();
    
    public App()
    {
        AllocConsole();

        Console.WriteLine("Quest Editor App started.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        QuestPackExplorer.QuestPackExplorerModel.Clear();
        base.OnExit(e);
    }
}

