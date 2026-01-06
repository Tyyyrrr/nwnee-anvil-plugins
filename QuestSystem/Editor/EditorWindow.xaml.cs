using System;
using System.Windows;
using QuestEditor.QuestCanvas;
using QuestEditor.QuestPackExplorer;
using QuestSystem;

namespace QuestEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class EditorWindow : Window
{    
    public EditorWindow()
    {
        InitializeComponent();
        
        var app = (App)Application.Current;

        ExplorerView.DataContext = app.ExplorerVM;
        CanvasView.DataContext = app.CanvasVM;

        Console.WriteLine("EditorWindow initialized.");
    }
}