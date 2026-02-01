using QuestEditor.Graph;
using QuestEditor.Nodes;
using QuestEditor.Shared;
using QuestSystem.Objectives;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

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
            this.model = (Objective)model.Clone();
            AreaTags = new(model.AreaTags);
            TriggerTags = new(model.TriggerTags);

            SourceID = parent.ID;
            TargetID = model.NextStageID;

            AreaTags.CollectionChanged += OnAreaTagsChanged;
            TriggerTags.CollectionChanged += OnTriggerTagsChanged;
        }

        private Objective snapshot;
        private Objective model;
        public virtual Objective Objective => model;

        protected override void Apply()
        {
            snapshot = model;
            model = (Objective)snapshot.Clone();
        }

        protected override IReadOnlyList<StatefulViewModelBase>? DirectDescendants => null;

        protected sealed class UpdateObjectiveOperation(ObjectiveVM origin, Objective before, Objective after, string propertyName) : UndoableOperation(origin)
        {
            readonly Objective _before = before;
            readonly Objective _after = after;
            readonly string _propertyName = propertyName;
            protected override void ProtectedDo() { }

            protected override void ProtectedRedo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.model = _after;
                vm.RaisePropertyChanged(_propertyName);
            }

            protected override void ProtectedUndo()
            {
                var vm = (ObjectiveVM)Origin;
                vm.model = _before;
                vm.RaisePropertyChanged(_propertyName);
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
                OutputChanged?.Invoke(this, value);
                RaisePropertyChanged(nameof(NextStageID));
                RaisePropertyChanged(nameof(NextStageIDString));
            }

        }

        public string NextStageIDString
        {
            get => NextStageID.ToString();
            set
            {
                if (!int.TryParse(value, out var nextID) || NextStageID == nextID)
                    return;

                PushOperation(new SetNextIDOperation(this, nextID));
            }
        }


        private sealed class SetNextIDOperation(ObjectiveVM node, int newVal) : UndoableOperation(node)
        {
            private readonly int _oldVal = node.NextStageID;
            private readonly int _newVal = newVal;
            protected override void ProtectedDo()
            {
                ((ObjectiveVM)Origin).NextStageID = _newVal;
            }

            protected override void ProtectedRedo() => ProtectedDo();
            protected override void ProtectedUndo()
            {
                ((ObjectiveVM)Origin).NextStageID = _oldVal;
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
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.CooldownTag = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof (CooldownTag)));

            }
        }

        public float CooldownDurationSeconds
        {
            get => ShowCooldownBox ? Objective.Cooldown!.DurationSeconds : -1;
            set
            {
                if(CooldownDurationSeconds == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.DurationSeconds = value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(CooldownDurationSeconds)));
            }
        }

        public Objective.ObjectiveTimer.JournalFormat CooldownFormat
        {
            get => ShowCooldownBox ? Objective.Cooldown!.Format : default;
            set
            {
                if(CooldownFormat == value) return;
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
                if(CooldownRunOffline == value) return;
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
                if (CooldownShowInJournal == value) return;
                var backup = (Objective)Objective.Clone();
                Objective.Cooldown ??= new();
                RaisePropertyChanged(nameof(ShowCooldownBox));
                Objective.Cooldown.ShowInJournal= value;
                PushOperation(new UpdateObjectiveOperation(this, backup, Objective, nameof(CooldownShowInJournal)));
            }
        }

        public int SourceID { get; }

        public int TargetID { get; private set; }

        public Point Position { get; }
    }
}