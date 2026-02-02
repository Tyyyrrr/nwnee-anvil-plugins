using QuestEditor.Explorer;
using QuestSystem.Nodes;

namespace QuestEditor.Nodes
{
    public sealed class CooldownNodeVM : SingleOutputNodeVM
    {
        public CooldownNodeVM(CooldownNode node, QuestVM quest) : base(node, quest)
        {
            var ts = TimeSpan.FromSeconds(Math.Round(node.DurationSeconds));
            _days = ts.Days;
            _hours = ts.Hours;
            _minutes = ts.Minutes;
            _seconds = ts.Seconds;
        }

        public override string NodeType => "Cooldown";

        protected override CooldownNode Node => (CooldownNode)base.Node;


        public string Days
        {
            get => _days.ToString();
            set
            {
                if (int.TryParse(value, out var i) && i >= 0 && SetProperty(ref _days, i))
                {
                    var before = (CooldownNode)Node.Clone();
                    SetTotalSeconds();
                    PushOperation(new UpdateNodeOperation(this, before, Node, nameof(RunOffline)));
                }
            }
        } int _days;

        public string Hours
        {
            get => _hours.ToString();
            set
            {
                if (int.TryParse(value, out var i) && i >= 0 && i < 24 && SetProperty(ref _hours, i))
                {
                    var before = (CooldownNode)Node.Clone();
                    SetTotalSeconds();
                    PushOperation(new UpdateNodeOperation(this, before, Node, nameof(RunOffline)));
                }
            }

        } int _hours;

        public string Minutes
        {
            get => _minutes.ToString();
            set
            {
                if (int.TryParse(value, out var i) && i >= 0 && i < 60 && SetProperty(ref _minutes, i))
                {
                    var before = (CooldownNode)Node.Clone();
                    SetTotalSeconds();
                    PushOperation(new UpdateNodeOperation(this, before, Node, nameof(RunOffline)));
                }
            }
        } int _minutes;

        public string Seconds
        {
            get => _seconds.ToString();
            set
            {
                if (int.TryParse(value, out var i) && i >= 0 && i < 60 && SetProperty(ref _seconds, i))
                {
                    var before = (CooldownNode)Node.Clone();
                    SetTotalSeconds();
                    PushOperation(new UpdateNodeOperation(this, before, Node, nameof(RunOffline)));
                }
            }
        } int _seconds;

        void SetTotalSeconds()
        {
            var ts = new TimeSpan(_days, _hours, _minutes, _seconds);
            Node.DurationSeconds = (int)Math.Round(ts.TotalSeconds);
        }

        public bool RunOffline
        {
            get => _runOffline;
            set
            {
                if (SetProperty(ref _runOffline, value))
                {
                    var before = (CooldownNode)Node.Clone();
                    Node.RunOffline = value;
                    PushOperation(new UpdateNodeOperation(this, before, Node, nameof(RunOffline)));
                }
            }
        } bool _runOffline;
    }
}
