namespace ClockPlugin.Cooldowns // todo: create a separate plugin for this
{
    public interface ICooldown
    {
        public string CooldownTag {get;}
        public float DurationSeconds {get;}
        public bool RunOffline {get;}
    }
}