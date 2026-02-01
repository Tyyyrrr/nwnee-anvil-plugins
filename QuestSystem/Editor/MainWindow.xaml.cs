using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;

namespace QuestEditor;

public partial class MainWindow : Window
{
    #region COMMANDS
    static void RouteExecute(ICommand cmd, ExecutedRoutedEventArgs e) => cmd?.Execute(e.Parameter);
    static void RouteCanExecute(ICommand cmd, CanExecuteRoutedEventArgs e) => e.CanExecute = cmd?.CanExecute(e.Parameter) == true;

    #region Explorer-only commands
    public static readonly DependencyProperty NewCommandProperty =
        DependencyProperty.Register(nameof(NewCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand NewCommand { get => (ICommand)GetValue(NewCommandProperty); set => SetValue(NewCommandProperty, value); }
    void New_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(NewCommand, e);
    void New_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(NewCommand, e);

    public static readonly DependencyProperty OpenCommandProperty =
        DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand OpenCommand { get => (ICommand)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty, value); }
    void Open_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(OpenCommand, e);
    void Open_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(OpenCommand, e);

    public static readonly DependencyProperty SaveCommandProperty =
        DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand SaveCommand { get => (ICommand)GetValue(SaveCommandProperty); set => SetValue(SaveCommandProperty, value); }
    void Save_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(SaveCommand, e);
    void Save_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(SaveCommand, e);

    public static readonly DependencyProperty SaveAsCommandProperty =
        DependencyProperty.Register(nameof(SaveAsCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand SaveAsCommand { get => (ICommand)GetValue(SaveAsCommandProperty); set => SetValue(SaveAsCommandProperty, value); }
    void SaveAs_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(SaveAsCommand, e);
    void SaveAs_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(SaveAsCommand, e);
    #endregion

    #region App-global commands
    public static readonly DependencyProperty HelpCommandProperty =
        DependencyProperty.Register(nameof(HelpCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand HelpCommand { get => (ICommand)GetValue(HelpCommandProperty); set => SetValue(HelpCommandProperty, value); }
    void Help_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(HelpCommand, e);
    void Help_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(HelpCommand, e);

    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand CloseCommand { get => (ICommand)GetValue(CloseCommandProperty); set => SetValue(CloseCommandProperty, value); }
    void Close_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(CloseCommand, e);
    void Close_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(CloseCommand, e);

    public static readonly DependencyProperty UndoCommandProperty =
        DependencyProperty.Register(nameof(UndoCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand UndoCommand { get => (ICommand)GetValue(UndoCommandProperty); set => SetValue(UndoCommandProperty, value); }
    void Undo_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(UndoCommand, e);
    void Undo_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(UndoCommand, e);

    public static readonly DependencyProperty RedoCommandProperty =
        DependencyProperty.Register(nameof(RedoCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand RedoCommand { get => (ICommand)GetValue(RedoCommandProperty); set => SetValue(RedoCommandProperty, value); }
    void Redo_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(RedoCommand, e);
    void Redo_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(RedoCommand, e);
    #endregion

    #region Context-specific commands
    public static readonly DependencyProperty SelectAllCommandProperty =
        DependencyProperty.Register(nameof(SelectAllCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand SelectAllCommand { get => (ICommand)GetValue(SelectAllCommandProperty); set => SetValue(SelectAllCommandProperty, value); }
    void SelectAll_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(SelectAllCommand, e);
    void SelectAll_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(SelectAllCommand, e);

    public static readonly DependencyProperty CopyCommandProperty =
        DependencyProperty.Register(nameof(CopyCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand CopyCommand { get => (ICommand)GetValue(CopyCommandProperty); set => SetValue(CopyCommandProperty, value); }
    void Copy_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(CopyCommand, e);
    void Copy_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(CopyCommand, e);

    public static readonly DependencyProperty CutCommandProperty =
        DependencyProperty.Register(nameof(CutCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand CutCommand { get => (ICommand)GetValue(CutCommandProperty); set => SetValue(CutCommandProperty, value); }
    void Cut_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(CutCommand, e);
    void Cut_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(CutCommand, e);

    public static readonly DependencyProperty PasteCommandProperty =
        DependencyProperty.Register(nameof(PasteCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand PasteCommand { get => (ICommand)GetValue(PasteCommandProperty); set => SetValue(PasteCommandProperty, value); }
    void Paste_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(PasteCommand, e);
    void Paste_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(PasteCommand, e);

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand DeleteCommand { get => (ICommand)GetValue(DeleteCommandProperty); set => SetValue(DeleteCommandProperty, value); }
    void Delete_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(DeleteCommand, e);
    void Delete_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(DeleteCommand, e);

    public static readonly DependencyProperty FindCommandProperty =
        DependencyProperty.Register(nameof(FindCommand), typeof(ICommand), typeof(MainWindow));
    public ICommand FindCommand { get => (ICommand)GetValue(FindCommandProperty); set => SetValue(FindCommandProperty, value); }
    void Find_Executed(object s, ExecutedRoutedEventArgs e) => RouteExecute(FindCommand, e);
    void Find_CanExecute(object s, CanExecuteRoutedEventArgs e) => RouteCanExecute(FindCommand, e);
    #endregion
    #endregion

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

        // Add the dialog frame style → removes the icon
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_DLGMODALFRAME);

        // Force non-client area to update
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE |
            NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_NOZORDER |
            NativeMethods.SWP_FRAMECHANGED);
    }


    #region Themes
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public void ToggleLightMode(object? _, RoutedEventArgs __)
    {
        int useDark = 0;
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

        // reapply chrome to update window looks
        var chrome = WindowChrome.GetWindowChrome(this);
        WindowChrome.SetWindowChrome(this, new());
        WindowChrome.SetWindowChrome(this, chrome);

        // swap application resource dictionaries
        ((App)Application.Current).SetLightTheme();

        LightThemeMenuOption.IsEnabled = false;
        DarkThemeMenuOption.IsEnabled = true;
    }
    public void ToggleDarkMode(object? _, RoutedEventArgs __)
    {
        int useDark = 1;
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

        //// reapply chrome to update window looks
        var chrome = WindowChrome.GetWindowChrome(this);
        WindowChrome.SetWindowChrome(this, new());
        WindowChrome.SetWindowChrome(this, chrome);

        // swap application resource dictionaries
        ((App)Application.Current).SetDarkTheme();

        LightThemeMenuOption.IsEnabled = true;
        DarkThemeMenuOption.IsEnabled = false;
    }


    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_DLGMODALFRAME = 0x0001;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attr,
            ref int attrValue,
            int attrSize);

    }

    #endregion
}
