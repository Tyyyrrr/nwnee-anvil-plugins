using QuestEditor.Shared;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public enum OutputMode
    {
        Default,
        Loop,
        Final
    }

    public class ConnectionOutputVM : ConnectionSocketVM
    {
        public int TargetID
        {
            get => _targetID;
            set
            {
                if (SetProperty(ref _targetID, value))
                {
                    if (_targetID == SourceID && Mode != OutputMode.Loop)
                        Mode = OutputMode.Loop;
                    else if (_targetID == -2 && Mode != OutputMode.Final)
                        Mode = OutputMode.Final;
                    else if ((_targetID >= 0 || _targetID == -1) && Mode != OutputMode.Default)
                        Mode = OutputMode.Default;
                }
            }
        } int _targetID;

        public OutputMode Mode
        {
            get => _mode;
            set
            {
                if (SetProperty(ref _mode, value))
                {
                    if (value == OutputMode.Loop && TargetID != SourceID)
                        _targetID = SourceID;
                    else if (value == OutputMode.Final && TargetID != -2)
                        _targetID = -2;
                    else if (value == OutputMode.Default && TargetID != -1)
                        _targetID = -1;

                    RaisePropertyChanged(nameof(SocketColor));
                    RaisePropertyChanged(nameof(SocketColorBrush));
                    RaisePropertyChanged(nameof(IsDefaultModeActive));
                    RaisePropertyChanged(nameof(IsDefaultModeInactive));
                    RaisePropertyChanged(nameof(IsLoopModeActive));
                    RaisePropertyChanged(nameof(IsLoopModeInactive));
                    RaisePropertyChanged(nameof(IsFinalModeActive));
                    RaisePropertyChanged(nameof(IsFinalModeInactive));
                    RaisePropertyChanged(nameof(CanBeTargeted));
                    ((RelayCommand)SetModeCommand).RaiseCanExecuteChanged();

                    ModeChanged?.Invoke(this);
                }
            }
        }
        OutputMode _mode = default;

        public bool IsDefaultModeActive => Mode == OutputMode.Default;
        public bool IsDefaultModeInactive => !IsDefaultModeActive;

        public bool IsLoopModeActive => Mode == OutputMode.Loop;
        public bool IsLoopModeInactive => !IsLoopModeActive;

        public bool IsFinalModeActive => Mode == OutputMode.Final;
        public bool IsFinalModeInactive => !IsFinalModeActive;

        public override Color SocketColor => Mode switch
        {
            OutputMode.Loop => Colors.Magenta,
            OutputMode.Final => Colors.Orange,
            _ => SocketColorBrush.Color
        };
        public override SolidColorBrush SocketColorBrush
        {
            get => Mode switch
            {
                OutputMode.Loop => Brushes.Magenta,
                OutputMode.Final => Brushes.Orange,
                _ => _socketColorBrush
            };
            set
            {
                if (SetProperty(ref _socketColorBrush, value))
                    RaisePropertyChanged(nameof(SocketColor));
            }
        }

        public override bool CanBeTargeted
        {
            get => _canBeTargeted && Mode == OutputMode.Default;
            set => SetProperty(ref _canBeTargeted, value);
        }


        public event Action<ConnectionOutputVM>? ModeChanged;

        public ConnectionOutputVM(int sourceID, int targetID) : base(sourceID)
        {
            SocketColorBrush = (SolidColorBrush)((App)Application.Current).Resources["OutputSocketBrush"];

            SetModeCommand = new RelayCommand(p => Mode = (OutputMode)p!, p => Mode != (OutputMode)p!);

            TargetID = targetID;
        }

        public ICommand SetModeCommand { get; }
    }
}
