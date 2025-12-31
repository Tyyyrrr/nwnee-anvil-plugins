using System;
using System.Windows;
using QuestEditor.QuestCanvas;
using QuestEditor.QuestPackExplorer;

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

        ExplorerView.ViewModel = app.ExplorerVM;
        //CanvasView.ViewModel = app.CanvasVM; // example

        Console.WriteLine("EditorWindow initialized.");
    }
}