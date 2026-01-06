using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QuestEditor.QuestPackExplorer;
using QuestEditor.Shared;
using QuestEditor.StageNode;

namespace QuestEditor.QuestCanvas
{
    public sealed class QuestCanvasViewModel : ViewModelBase
    {        
        private static readonly SolidColorBrush _canvasBorderBrush = new(Color.FromRgb(15,15,15) * 5);
        private static readonly SolidColorBrush _canvasBackgroundBrush = new(Color.FromRgb(25,20,45) * 4);
        public Brush CanvasBorderBrush => _canvasBorderBrush;
        public Brush CanvasBackgroundBrush => _canvasBackgroundBrush;

        public bool IsEnabled {get => _isEnabled; set
            {
                _isEnabled=value;
                if(!value){
                    SetModel(null);
                }
                OnPropertyChanged(nameof(IsEnabled));
            }} private bool _isEnabled = false;

        public ICommand AddStageCommand {get;}
        public ICommand RemoveStageCommand {get;}

        private bool _overlayCapturesInput = false;
        public bool OverlayCapturesInput {get => _overlayCapturesInput;
            set
                {
                    if(_overlayCapturesInput==value) return;
                    _overlayCapturesInput=value;
                    OnPropertyChanged(nameof(OverlayCapturesInput));
                }
            }

        public QuestCanvasViewModel()
        {
            AddStageCommand = new RelayCommand(AddStage,_=>true);
            RemoveStageCommand = new RelayCommand(RemoveStage,_=>true);
        }

        public QuestCanvasModel? ApplyChangesToModel()
        {
            Console.WriteLine("Applying changes...");

            if(_model == null)
            {
                Console.WriteLine("Model is null");
                return null;
            }

            _model.Quest.Name = QuestName;

            _model.Stages.Clear();

            foreach(var node in StageNodes)
            {
                var questStage = node.GetQuestStage();

                _model.Stages.Add(questStage.ID, questStage);   
            }
            
            Console.WriteLine("Changes applied. New stages count: " + _model.Stages.Count);

            return _model;
        }

        void AddStage(object? p)
        {
            if(p is not Point point)
                point = default;

            Console.WriteLine("Adding stage to main canvas at point " + point);

            var viewModel = new StageNodeViewModel(new(),this);

            viewModel.PropertyChanged += StageNodePropertyChanged;

            viewModel.X = point.X;
            viewModel.Y = point.Y;
            
            StageNodes.Add(viewModel);

            OverlayCapturesInput = false;
        }

        void RemoveStage(object? s)
        {
            if(s is not StageNodeViewModel vm)
                return;

            Console.WriteLine("Removing stage " + vm.StageID);

            vm.PropertyChanged -= StageNodePropertyChanged;

            if (!StageNodes.Remove(vm))
            {
                Console.WriteLine("Failed ot remove stage node!");
            }
        }

        public string QuestName
        {
            get => _questName;
            set {
                if(_questName == value) return;
                _questName = value;
                OnPropertyChanged(nameof(QuestName));
            }
        } private string _questName = string.Empty;
        
        public ObservableCollection<StageNodeViewModel> StageNodes {get;set;} = [];
        public int NodesCount => StageNodes.Count;

        public double CanvasWidth => Math.Max(3000,StageNodes.Max(n => n.X + 300));
        public double CanvasHeight => Math.Max(2000,StageNodes.Max(n => n.Y + 200));

        private StageNodeViewModel? _currentlyEditedNode = null;
        public StageNodeViewModel? CurrentlyEditedNode
        {
            get=>_currentlyEditedNode;
            set
            {
                if(_currentlyEditedNode == value)return;
                _currentlyEditedNode = value;
                OnPropertyChanged(nameof(CurrentlyEditedNode));
            }
        }

        private QuestCanvasModel? _model;
        public void SetModel(QuestCanvasModel? model, QuestEditorMetadata? meta = null)
        {
            if(_model == model) return;

            CurrentlyEditedNode = null;

            QuestName = string.Empty;

            foreach(var node in StageNodes)
                node.PropertyChanged -= StageNodePropertyChanged;

            StageNodes.Clear();

            _model = model;

            if(model != null)
            {
                QuestName=model.Quest.Name;
                foreach(var stage in model.Stages.Values)
                {
                    var node = new StageNodeViewModel(stage,this);
                    if(meta != null && meta.NodePositions.TryGetValue(node.StageID,out var pos))
                    {
                        node.X=pos.X;
                        node.Y=pos.Y;
                    }
                    node.PropertyChanged += StageNodePropertyChanged;
                    StageNodes.Add(node);
                }
            }

            OnPropertyChanged(nameof(StageNodes));
            OnPropertyChanged(nameof(QuestName));
            OnPropertyChanged(nameof(NodesCount));

            IsEnabled = model != null;
        }

        void StageNodePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(StageNodeViewModel.JournalEntryExpanded))
            {
                if (s is not StageNodeViewModel vm) return;
                bool focused = vm.JournalEntryExpanded;
                if(CurrentlyEditedNode != null)
                    CurrentlyEditedNode.JournalEntryExpanded = false;

                CurrentlyEditedNode = focused ? vm : null;
                return;
            }

            if(e.PropertyName != nameof(StageNodeViewModel.X) && e.PropertyName != nameof(StageNodeViewModel.Y)) return;

            OnPropertyChanged(nameof(CanvasHeight));
            OnPropertyChanged(nameof(CanvasWidth));
        }
    }
}