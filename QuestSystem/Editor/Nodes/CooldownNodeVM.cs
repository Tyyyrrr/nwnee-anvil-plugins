using QuestEditor.Explorer;
using QuestEditor.Shared;
using QuestSystem.Nodes;
using System.Diagnostics;
using System.Xml.Linq;

namespace QuestEditor.Nodes
{
    public sealed class CooldownNodeVM(CooldownNode node, QuestVM quest) : SingleOutputNodeVM(node, quest)
    {
        public override string NodeType => "Cooldown";

        protected override CooldownNode Node => (CooldownNode)base.Node;


        public string CooldownTag
        {
            get => Node.CooldownTag;
            set
            {
                if (Node.CooldownTag == value) return;
                var before = (CooldownNode)Node.Clone();
                Node.CooldownTag = value;
                PushOperation(new UpdateNodeOperation(this, before, Node, nameof(CooldownTag)));
            }
        }

        public bool RunOffline
        {
            get => Node.RunOffline;
            set
            {
                if (Node.RunOffline != value)
                {
                    var before = (CooldownNode)Node.Clone();
                    Node.RunOffline = value;
                    PushOperation(new UpdateNodeOperation(this, before, Node, nameof(RunOffline)));
                }
            }
        }

        private sealed class SetDurationOperation(CooldownNodeVM node, TimeSpan oldDuration, TimeSpan newDuration) : UndoableOperation(node)
        {
            protected override void ProtectedDo()
            {
                node.Node.DurationSeconds = (float)newDuration.TotalSeconds;
                node.RaisePropertyChanges();
            }

            protected override void ProtectedRedo() => ProtectedDo();

            protected override void ProtectedUndo()
            {
                node.Node.DurationSeconds = (float)oldDuration.TotalSeconds;
                node.RaisePropertyChanges();
            }
        }


        void RaisePropertyChanges()
        {
            RaisePropertyChanged(nameof(Days));
            RaisePropertyChanged(nameof(Hours));
            RaisePropertyChanged(nameof(Minutes));
            RaisePropertyChanged(nameof(Seconds));
        }

        public string Days
        {
            get => TimeSpan.FromSeconds(Node.DurationSeconds).Days.ToString();
            set
            {
                if (value != Days && int.TryParse(value, out var i) && i >= 0)
                {
                    var oldDuration = TimeSpan.FromSeconds(Node.DurationSeconds);
                    var newDuration = new TimeSpan(i,oldDuration.Hours,oldDuration.Minutes,oldDuration.Seconds);
                    PushOperation(new SetDurationOperation(this, oldDuration, newDuration));
                }
            }
        }


        public string Hours
        {
            get => TimeSpan.FromSeconds(Node.DurationSeconds).Hours.ToString();
            set
            {
                if (value != Hours && int.TryParse(value, out var i) && i >= 0 && i < 24)
                {
                    var oldDuration = TimeSpan.FromSeconds(Node.DurationSeconds);
                    var newDuration = new TimeSpan(oldDuration.Days, i, oldDuration.Minutes, oldDuration.Seconds);
                    PushOperation(new SetDurationOperation(this, oldDuration, newDuration));
                }
            }
        }

        public string Minutes
        {
            get => TimeSpan.FromSeconds(Node.DurationSeconds).Minutes.ToString();
            set
            {
                if (value != Minutes && int.TryParse(value, out var i) && i >= 0 && i < 60)
                {
                    var oldDuration = TimeSpan.FromSeconds(Node.DurationSeconds);
                    var newDuration = new TimeSpan(oldDuration.Days, oldDuration.Hours, i, oldDuration.Seconds);
                    PushOperation(new SetDurationOperation(this, oldDuration, newDuration));
                }
            }
        }

        public string Seconds
        {
            get => TimeSpan.FromSeconds(Node.DurationSeconds).Seconds.ToString();
            set
            {
                if (value != Seconds && int.TryParse(value, out var i) && i >= 0 && i < 60)
                {
                    var oldDuration = TimeSpan.FromSeconds(Node.DurationSeconds);
                    var newDuration = new TimeSpan(oldDuration.Days, oldDuration.Hours, oldDuration.Minutes, i);
                    PushOperation(new SetDurationOperation(this, oldDuration, newDuration));
                }
            }
        }
    }
}
