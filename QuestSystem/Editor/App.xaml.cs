using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using QuestEditor.Explorer;
using QuestEditor.Graph;
using QuestEditor.Inspector;

namespace QuestEditor;

public partial class App : Application
{
    public readonly UserPrefs UserPreferences;
    public sealed class UserPrefs
    {
        public string PacksDirectory { get; set; } = string.Empty;
        public bool DarkMode { get; set; } = false;
    }

    private readonly List<IDisposable> disposables = new();
    private readonly List<IAsyncDisposable> asyncDisposables = new();

    public App()
    {
        UserPreferences=LoadUserPrefs();

        this.Exit += async (_, _) =>
        {
            foreach (var asyncDisposable in asyncDisposables)
                await asyncDisposable.DisposeAsync();
            
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveUserPrefs(UserPreferences);

        foreach (var disposable in disposables)
            disposable.Dispose();

        base.OnExit(e);
    }

    private static string? GetPrefsPath()
    {
        string? path = typeof(App).Assembly.Location;
        if (path == null) return null;
        path = Path.GetDirectoryName(path);
        if (path == null) return null;
        path = Path.Combine(path, "userPrefs.json");
        return path;
    }
    static void SaveUserPrefs(UserPrefs prefs)
    {
        var path = GetPrefsPath() ?? throw new InvalidOperationException("Failed to get UserPrefs filepath");

        var stream = File.Exists(path) ? File.Open(path, FileMode.Truncate, FileAccess.Write) : File.Open(path, FileMode.CreateNew, FileAccess.Write);
        var sw = new StreamWriter(stream, leaveOpen:false);
        var json = JsonSerializer.Serialize(prefs);
        sw.Write(json);
        sw.Flush();
        sw.Dispose();
    }
    static UserPrefs LoadUserPrefs()
    {
        var path = GetPrefsPath() ?? throw new InvalidOperationException("Failed to get UserPrefs filepath");

        UserPrefs prefs;
        if(!File.Exists(path))
        {
            prefs = new();
            SaveUserPrefs(prefs);
        }
        else
        {
            var fs = File.OpenRead(path);
            var sr = new StreamReader(fs,leaveOpen:false);
            var json = sr.ReadToEnd();
            sr.Dispose();
            prefs = JsonSerializer.Deserialize<UserPrefs>(json) ?? new();
        }
        return prefs;
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mw = new MainWindow();

        MainWindow = mw;
        MainWindow.Show();

        if (UserPreferences.DarkMode)
            mw.ToggleDarkMode(null, new());
        else mw.ToggleLightMode(null, new());

        var mwvm = (MainWindowVM)mw.DataContext;

        disposables.Add(mwvm.Explorer);
        asyncDisposables.Add(mwvm.Explorer);
    }


    public void SetLightTheme()
    {
        Resources.MergedDictionaries.Clear();

        var themeResourceDictionary = new ResourceDictionary
        {
            Source = new Uri($"/Themes/Light.xaml", UriKind.Relative)
        };

        Resources.MergedDictionaries.Add(themeResourceDictionary);

        UserPreferences.DarkMode = false;
    }
    public void SetDarkTheme()
    {
        Resources.MergedDictionaries.Clear();

        var themeResourceDictionary = new ResourceDictionary
        {
            Source = new Uri($"/Themes/Dark.xaml", UriKind.Relative)
        };

        Resources.MergedDictionaries.Add(themeResourceDictionary);

        UserPreferences.DarkMode = true;
    }

}

