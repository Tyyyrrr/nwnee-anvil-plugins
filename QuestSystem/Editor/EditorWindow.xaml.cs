using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuestEditor.QuestCanvas;
using QuestEditor.QuestPackExplorer;

namespace QuestEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class EditorWindow : Window
{    
    public QuestPackExplorerViewModel ExplorerVM { get; }
    public QuestCanvasViewModel CanvasVM { get; }

    public EditorWindow()
    {
        InitializeComponent();

        ExplorerVM = new();
        CanvasVM = new(ExplorerVM);

        PackExplorer.DataContext=ExplorerVM;
        EditorCanvas.DataContext=CanvasVM;
        
        Console.WriteLine("EditorWindow initialized.");
    }
}