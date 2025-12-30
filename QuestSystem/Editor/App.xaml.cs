using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuestEditor;

public partial class App : Application
{
    private static readonly bool _useConsole = Environment.GetCommandLineArgs().Contains("console", StringComparer.OrdinalIgnoreCase);

    private readonly TextWriter _logWriter;

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern bool AllocConsole();

    public App()
    {
        this.DispatcherUnhandledException += OnDispatcherUnhandledException;

        var logFilePath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(App))?.Location);

        ArgumentException.ThrowIfNullOrEmpty(logFilePath);

        logFilePath = Path.Combine(logFilePath,"EditorLog.txt");

        var fileStream = File.Exists(logFilePath)
            ? File.Open(logFilePath,FileMode.Truncate,FileAccess.Write, FileShare.Read)
            : File.Open(logFilePath,FileMode.CreateNew,FileAccess.Write, FileShare.Read);

        if (_useConsole)
        {
            AllocConsole();
            _logWriter = TextWriter.Synchronized(new MultiTextWriter(Console.Out, new StreamWriter(fileStream)));
        }
        else
        {
            _logWriter = TextWriter.Synchronized(new StreamWriter(fileStream));
        }

        Console.SetOut(_logWriter);

        Console.WriteLine("Quest Editor App started.");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Console.WriteLine("Unhandled exception: " + e.Exception);  
        if(_useConsole){
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        QuestPackExplorer.QuestPackExplorerModel.Clear();
        _logWriter.Dispose();
        base.OnExit(e);
    }
}

public sealed class MultiTextWriter(params TextWriter[] writers) : TextWriter
{
    private readonly TextWriter[] _writers = writers;

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) { foreach (var w in _writers) w.Write(value); }
    public override void Write(string? value) { foreach (var w in _writers) w.Write(value); }
    public override void WriteLine(string? value) { foreach (var w in _writers) w.WriteLine(value); }
    public override void Flush() { foreach (var w in _writers) w.Flush(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var w in _writers)
                w.Dispose();
        }
        base.Dispose(disposing);
    }
}
