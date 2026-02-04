using QuestEditor.Graph;
using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem.Objectives;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace QuestEditor.Objectives
{
    public abstract class ObjectiveVM : StatefulViewModelBase
    {
        public ConnectionOutputVM OutputVM
        {
            get => _outputVM;
            set => SetProperty(ref _outputVM, value);
        } ConnectionOutputVM _outputVM;

        public static ObjectiveVM? SelectViewModel(Objective objective, StageNodeVM node)
        {
            return objective switch
            {
                ObjectiveInteract interact => new ObjectiveInteractVM(interact, node),
                ObjectiveKill kill => new ObjectiveKillVM(kill, node),
                ObjectiveDeliver deliver => new ObjectiveDeliverVM(deliver, node),
                ObjectiveObtain obtain => new ObjectiveObtainVM(obtain, node),
                ObjectiveExplore explore => new ObjectiveExploreVM(explore, node),
                ObjectiveSpellcast spellcast => new ObjectiveSpellcastVM(spellcast, node),
                _ => null
            };
        }


        public ObjectiveVM(Objective model, StageNodeVM parent) : base(parent)
        {
            snapshot = model;
            _outputVM = new(parent.ID, model.NextStageID);
            _outputVM.CanBeTargeted = true;
            _outputVM.ModeChanged += o => SetNextStageID(o.TargetID);

            this.model = (Objective)model.Clone();
            AreaTags = new(model.AreaTags);
            TriggerTags = new(model.TriggerTags);

            SourceID = parent.ID;
            TargetID = model.NextStageID;

            AreaTags.CollectionChanged += OnAreaTagsChanged;
            TriggerTags.CollectionChanged += OnTriggerTagsChanged;

            AddAreaTagCommand = new RelayCommand(AddAreaTag, _ => true);
            AddTriggerTagCommand = new RelayCommand(AddTriggerTag, _ => true);
            RemoveAreaTagCommand = new RelayCommand(RemoveAreaTag, _ => true);
            RemoveTriggerTagCommand = new RelayCommand(RemoveTriggerTag, _ => true);

            DeleteObjectiveCommand = new RelayCommand(o => parent.RemoveObjective(o), _ => true);
        }

        private Objective snapshot;
        private Objective model;
        public virtual Objective Objective => model;

        public abstract string ObjectiveType { get; }

        public ICommand AddAreaTagCommand { get; }
        void AddAreaTag(object? parameter)
        {
            if (parameter is not string str || AreaTags.Contains(str)) return;
            AreaTags.Add(str);
        }
        public ICommand RemoveAreaTagCommand { get; }
        void RemoveAreaTag(object? parameter)
        {
            if(parameter is not string str) return;
            AreaTags.Remove(str);
        }
        public ICommand AddTriggerTagCommand { get; }
        void AddTriggerTag(object? parameter)
        {
            if( parameter is not string str || TriggerTags.Contains(str)) return;
            TriggerTags.Add(str);
        }
        public ICommand RemoveTriggerTagCommand { get; }
        void RemoveTriggerTag(object? parameter)
        {
            if (parameter is not string str) return;
            TriggerTags.Remove(str);
        }

        public ICommand DeleteObjectiveCommand { get; }


        protected override void Apply()
        {
            snapshot = model;
            model = (Objective)snapshot.Clone();
        }

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => null;

        protected sealed class UpdateObjectiveOperation(ObjectiveVM origin, Objective before, Objective after, params string[] propertyNames) : UndoableOperation(origin)
        {
            readonly Objective _before = before;
            readonly Objective _after = after;
            readonly string[] _propertyNames = propertyNames;
            protected override void ProtectedDo() { }

            protected override void ProtectedRedo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.model = _after;
                foreach(var pn in _propertyNames)
                    vm.RaisePropertyChanged(pn);
            }

            protected override void ProtectedUndo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.model = _before;
                foreach (var pn in _propertyNames)
                    vm.RaisePropertyChanged(pn);
            }
        }

        public ObservableCollection<string> AreaTags { get; private set; } = [];

        private sealed class UpdateAreaTagsOperation(ObjectiveVM origin, string[] oldTags, string[] newTags) : UndoableOperation(origin)
        {
            private readonly string[] _oldTags = oldTags;
            private readonly string[] _newTags = newTags;

            protected override void ProtectedDo() { ((ObjectiveVM)Origin).Objective.AreaTags = _newTags; }
            protected override void ProtectedRedo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.Objective.AreaTags = _newTags;
                vm.AreaTags.CollectionChanged -= vm.OnAreaTagsChanged;
                vm.RaisePropertyChanged(nameof(AreaTags));
                vm.AreaTags.CollectionChanged += vm.OnAreaTagsChanged;
            }

            protected override void ProtectedUndo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.Objective.AreaTags = _oldTags;
                vm.AreaTags.CollectionChanged -= vm.OnAreaTagsChanged;
                vm.AreaTags = new(_oldTags);
                vm.RaisePropertyChanged(nameof(AreaTags));
                vm.AreaTags.CollectionChanged += vm.OnAreaTagsChanged;
            }
        }
        void OnAreaTagsChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (Objective.AreaTags.SequenceEqual(AreaTags)) return;
            var oldTags = Objective.AreaTags.ToArray();
            var newTags = AreaTags.ToArray();
            PushOperation(new UpdateAreaTagsOperation(this, oldTags, newTags));
        }

        public ObservableCollection<string> TriggerTags { get; private set; } = [];
        private sealed class UpdateTriggerTagsOperation(ObjectiveVM origin, string[] oldTags, string[] newTags) : UndoableOperation(origin)
        {
            private readonly string[] _oldTags = oldTags;
            private readonly string[] _newTags = newTags;

            protected override void ProtectedDo()
            {
                ((ObjectiveVM)Origin).Objective.TriggerTags = _newTags;
            }

            protected override void ProtectedRedo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.Objective.TriggerTags = _newTags;
                vm.TriggerTags.CollectionChanged -= vm.OnTriggerTagsChanged;
                vm.TriggerTags = new(_newTags);
                vm.RaisePropertyChanged(nameof(TriggerTags));
                vm.TriggerTags.CollectionChanged += vm.OnTriggerTagsChanged;
            }

            protected override void ProtectedUndo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.Objective.TriggerTags = _oldTags;
                vm.TriggerTags.CollectionChanged -= vm.OnTriggerTagsChanged;
                vm.TriggerTags = new(_oldTags); 
                vm.RaisePropertyChanged(nameof(TriggerTags));
                vm.TriggerTags.CollectionChanged += vm.OnTriggerTagsChanged;
            }
        }

        void OnTriggerTagsChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (Objective.TriggerTags.SequenceEqual(TriggerTags)) return;
            var oldTags = Objective.TriggerTags.ToArray();
            var newTags = TriggerTags.ToArray();
            PushOperation(new UpdateTriggerTagsOperation(this, oldTags, newTags));
        }


        public string JournalEntry
        {
            get => Objective.JournalEntry;
            set
            {
                if (Objective.JournalEntry == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.JournalEntry = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(JournalEntry)));
            }
        }

        public bool ShowInJournal
        {
            get => Objective.ShowInJournal;
            set
            {
                if(Objective.ShowInJournal == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.ShowInJournal = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(ShowInJournal)));
            }
        }

        public event Action<ObjectiveVM, int>? OutputChanged;

        public int NextStageID
        {
            get => Objective.NextStageID;
            set
            {
                if (Objective.NextStageID == value) return;

                Objective.NextStageID = value;
                OutputVM.TargetID = value;
                RaisePropertyChanged(nameof(NextStageID));
                RaisePropertyChanged(nameof(NextStageIDString));
                OutputChanged?.Invoke(this, value);
            }

        }


        bool pushLock = false;
        public void SetNextStageID(int nextStageID)
        {
            if(pushLock || NextStageID == nextStageID) return;
            PushOperation(new SetNextIDOperation(this, nextStageID));
        }

        public string NextStageIDString
        {
            get => NextStageID.ToString();
            set
            {
                if (pushLock || !int.TryParse(value, out var nextID) || NextStageID == nextID)
                    return;

                PushOperation(new SetNextIDOperation(this, nextID));
            }
        }


        private sealed class SetNextIDOperation(ObjectiveVM objective, int newVal) : UndoableOperation(objective)
        {
            private readonly int _oldVal = objective.NextStageID;
            private readonly int _newVal = newVal;
            protected override void ProtectedDo()
            {
                objective.pushLock = true;
                objective.NextStageID = _newVal;
                objective.pushLock = false;
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                objective.pushLock = true;
                objective.NextStageID = _oldVal;
                objective.pushLock = false;
            }
        }




        public bool PartyMembersAllowed
        {
            get => Objective.PartyMembersAllowed;
            set
            {
                if (Objective.PartyMembersAllowed == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.PartyMembersAllowed = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(PartyMembersAllowed)));
            }
        }


        public bool ShowCooldownBox => Objective.Cooldown != null;

        public string CooldownTag
        {
            get => ShowCooldownBox ? Objective.Cooldown!.CooldownTag : string.Empty;
            set
            {
                if (CooldownTag == value) return;
                var backup = (Objective)Objective.Clone();
                if (string.IsNullOrWhiteSpace(value) || value == string.Empty)
                {
                    Objective.Cooldown = null;
                }
                else
                {
                    Objective.Cooldown ??= new();
                    Objective.Cooldown.CooldownTag = value;
                }
                RaisePropertyChanged(nameof(ShowCooldownBox));
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof (CooldownTag), nameof(ShowCooldownBox)));

            }
        }

        public float CooldownDurationSeconds
        {
            get => ShowCooldownBox ? Objective.Cooldown!.DurationSeconds : -1;
            set
            {
                if(!ShowCooldownBox || CooldownDurationSeconds == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.DurationSeconds = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(CooldownDurationSeconds), nameof(CooldownDurationSecondsString)));
            }
        }

        public string CooldownDurationSecondsString
        {
            get => CooldownDurationSeconds.ToString();
            set
            {
                if(float.TryParse(value, out var f) && f != CooldownDurationSeconds)
                {
                    CooldownDurationSeconds = f;
                    RaisePropertyChanged(nameof(CooldownDurationSecondsString));
                }
            }
        } 

        public Objective.ObjectiveTimer.JournalFormat CooldownFormat
        {
            get => ShowCooldownBox ? Objective.Cooldown!.Format : default;
            set
            {
                if(!ShowCooldownBox || CooldownFormat == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.Format = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(CooldownFormat)));
            }
        }

        public bool CooldownRunOffline
        {
            get => ShowCooldownBox && Objective.Cooldown!.RunOffline;
            set
            {
                if(!ShowCooldownBox || CooldownRunOffline == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.RunOffline = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(CooldownRunOffline)));
            }
        }

        public bool CooldownShowInJournal
        {
            get => ShowCooldownBox && Objective.Cooldown!.ShowInJournal;
            set
            {
                if (!ShowCooldownBox || CooldownShowInJournal == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.ShowInJournal= value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(CooldownShowInJournal)));
            }
        }

        public int SourceID { get; }

        public int TargetID { get; private set; }

        private static readonly IReadOnlyList<Objective.ObjectiveTimer.JournalFormat> _cooldownFormats = Enum.GetValues<Objective.ObjectiveTimer.JournalFormat>();
        public IReadOnlyList<Objective.ObjectiveTimer.JournalFormat> CooldownFormats => _cooldownFormats;
    }
}