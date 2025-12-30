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

namespace QuestEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        try{
        InitializeComponent();
        }catch(Exception ex)
        {
            Console.WriteLine("Error initializing EditorWindow: " + ex.ToString());
            throw;
        }
        finally
        {
            Console.ReadKey();
        }
        Console.WriteLine("EditorWindow initialized.");
    }
}