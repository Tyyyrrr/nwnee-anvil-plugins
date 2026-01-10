using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using QuestEditor.ObjectiveBox;
using QuestEditor.QuestCanvas;
using QuestEditor.RewardBox;
using QuestEditor.Shared;
using QuestSystem;
using QuestSystem.Objectives;

namespace QuestEditor.StageNode
{
    public sealed class StageNodeViewModel : ViewModelBase
    {
        private readonly QuestCanvasViewModel _parent;
        private readonly QuestStage _model;

        private ObjectiveBoxViewModel? _selectedObjective;
        public ObjectiveBoxViewModel? SelectedObjective
        {
            get => _selectedObjective;
            set
            {
                if(_selectedObjective==value) return;
                _selectedObjective = value;
                OnPropertyChanged(nameof(SelectedObjective));
                (DeleteObjectiveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<ObjectiveBoxViewModel> Objectives {get;set;} = [];

        public QuestStage GetQuestStage()
        {
            var stage = _model;
            var objectives = Objectives.Select(o=>o.GetQuestObjective()).Where(o=>o!=null).ToArray();
            stage.Objectives = objectives!;
            stage.ShowInJournal = ShowInJournal;
            stage.Reward = Reward.GetQuestStageReward() ?? new();//Reward.GetT<QuestStageReward>() ?? new();
            Console.WriteLine($"Got quest stage out from viewmodel. ID: {stage.ID}, objectives count: {stage.Objectives.Length}");
            return stage;
        }

        private string _journalEntry = string.Empty;
        public string JournalEntry
        {
            get => _journalEntry;
            set
            {
                if(_journalEntry.Length != value.Length)
                {
                    _journalEntry = value;
                    _model.JournalEntry = value;
                    OnPropertyChanged(nameof(JournalEntry));
                }
            }
        }

        private bool _showInJournal;
        public bool ShowInJournal
        {
            get=>_showInJournal;
            set
            {
                if(_showInJournal != value)
                {
                    _showInJournal = value;
                    _model.ShowInJournal = value;
                    if(!value) JournalEntryExpanded = false;
                    OnPropertyChanged(nameof(ShowInJournal));
                }
            }
        }

        private bool _journalEntryExpanded;
        public bool JournalEntryExpanded
        {
            get=>_journalEntryExpanded;
            set
            {
                if(value == _journalEntryExpanded) return;
                _journalEntryExpanded = value;
                OnPropertyChanged(nameof(JournalEntryExpanded));
            }
        }
        
        // public PropertyListViewModel Reward {get;} = new(new QuestStageReward()){Header="Stage Reward"};
        public RewardBoxViewModel Reward {get;} = new(new QuestStageReward());

        public ICommand NewObjectiveCommand {get;}
        public ICommand DeleteObjectiveCommand{get;}

        private static readonly SolidColorBrush _errorColorBrush = new(Color.FromRgb(200,0,0));
        private static readonly SolidColorBrush _warningColorBrush = new(Color.FromRgb(150,150,0));
        private static readonly SolidColorBrush _okColorBrush = new(Color.FromRgb(0,0,0));

        private static readonly Type[] _objectiveTypes = Assembly.GetAssembly(typeof(Quest))?
            .GetExportedTypes()
            .Where(t=>t.IsSubclassOf(typeof(Objective)) && t.IsSealed)
            .ToArray() ?? [];
        public ObservableCollection<string> ObjectiveTypeNames {get;} = new(_objectiveTypes.Select(t=>t.Name));
        
        private string _selectedNewObjectiveTypeName = string.Empty;
        public string SelectedNewObjectiveTypeName
        {
            get=>_selectedNewObjectiveTypeName;
            set
            {
                if(_selectedNewObjectiveTypeName == value) return;
                _selectedNewObjectiveTypeName = value;
                OnPropertyChanged(nameof(SelectedNewObjectiveTypeName));
                (NewObjectiveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        private Brush _nextStageIDColor = _okColorBrush;
        public Brush NextStageIDColor {get=>_nextStageIDColor;
        private set
            {
                if(_nextStageIDColor == value) return;

                _nextStageIDColor = value;
                OnPropertyChanged(nameof(NextStageIDColor));
            }
        }

        public StageNodeViewModel(QuestStage model, QuestCanvasViewModel parent)
        {
            _parent = parent;
            _model = model;

            NewObjectiveCommand = new RelayCommand(NewObjective,CanAddNewObjective);
            DeleteObjectiveCommand = new RelayCommand(DeleteObjective,CanDeleteObjective);

            int stageId = 0;

            var orderedIDs = _parent.StageNodes.Select(sn=>sn.StageID).Order().ToArray();


            for(int i = 0; i < orderedIDs.Length; i++)
            {
                if(i==0 && orderedIDs[i] > 0)
                {
                    break;
                }
                else if (i == orderedIDs.Length - 1)
                {
                    stageId = orderedIDs[i]+1;
                    break;
                }
                else if(orderedIDs[i+1]-1 > orderedIDs[i])
                {
                    stageId = orderedIDs[i]+1;
                    break;
                }
            }


            StageID=stageId;

            Reward = new(_model.Reward);

            JournalEntry=_model.JournalEntry;

            NextStageID = _model.NextStageID.ToString();

            Objectives = new(_model.Objectives.Select(o=>new ObjectiveBoxViewModel(o)));
            
            ShowInJournal = _model.ShowInJournal;
        }

        void NewObjective(object? _)
        {
            var type = _objectiveTypes.FirstOrDefault(t=>t.Name==SelectedNewObjectiveTypeName) ?? throw new InvalidOperationException("This command should not canExecute in current state");
        
            var obj = (Activator.CreateInstance(type) as Objective) ?? throw new InvalidOperationException("Can't create instance of type " + type.Name);

            var vm = new ObjectiveBoxViewModel(obj);

            Objectives.Add(vm);

            _model.Objectives = new Objective[Objectives.Count];

            _model.Objectives = Objectives.Select(o=>o.GetQuestObjective()).Where(o=>o!=null).ToArray()!;
        }

        bool CanAddNewObjective(object? _)
        {
            return ObjectiveTypeNames.Contains(SelectedNewObjectiveTypeName);
        }

        void DeleteObjective(object? _)
        {
            var obj = SelectedObjective;
            if(obj == null) return;
            Objectives.Remove(obj);
            
            _model.Objectives = new Objective[Objectives.Count];

            _model.Objectives = Objectives.Select(o=>o.GetQuestObjective()).Where(o=>o!=null).ToArray()!;
        }


        bool CanDeleteObjective(object? _)
        {
            return SelectedObjective != null;
        }
        private double _x;
        public double X {get=>_x;
            set {
                if (value != _x)
                {
                    _x = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }

        private double _y;
        public double Y {get=>_y;
            set
            {
                if(value != _y)
                {
                    _y=value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }

        public int StageID
        {
            get => _model.ID;
            set
            {
                if(_model.ID != value)
                {
                    _model.ID = value;
                    OnPropertyChanged(nameof(StageID));
                    OnPropertyChanged(nameof(StageIDLabelDisplay));
                }
            }
        }

        private string _nextStageID = string.Empty;
        public string NextStageID
        {
            get => _nextStageID;
            set
            {
                if(!int.TryParse(value, out var id))
                {
                    NextStageIDColor = _errorColorBrush;
                    if(_nextStageID != value)
                    {
                        _nextStageID = value;
                        OnPropertyChanged(nameof(NextStageID));
                        OnPropertyChanged(nameof(NextStageIDLabelDisplay));
                    }
                    return;
                }
                
                if(!_parent.StageNodes.Any(s=>s._model.ID == id))
                {
                    NextStageIDColor = _warningColorBrush;
                }
                else{
                    NextStageIDColor = _okColorBrush;
                }
                
                _nextStageID = value;
                _model.NextStageID = id;
                OnPropertyChanged(nameof(NextStageID));
                OnPropertyChanged(nameof(NextStageIDLabelDisplay));
            }
        }
        public string StageIDLabelDisplay => $"Stage ID: {StageID}";
        public string NextStageIDLabelDisplay => $"Next Stage ID: {_model.NextStageID}";
        public string NextStageIDToolTip => 
        @"Correct Values

        0 or greater:
            Complete the stage, grant reward and automatically continue to the next stage. (or loop if NextID == ID)
        -1:
            Complete the stage, grant reward, but do NOT continue.
        -2:
            Complete the quest on this stage.

        The same rule apply to objectives.";
    }
}