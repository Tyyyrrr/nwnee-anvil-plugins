using System;

namespace QuestSystem
{
    public sealed class Quest
    {
        public string Tag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        private QuestPack? _pack = null;
        internal QuestPack? Pack
        {
            get => _pack;
            set
            {
                if(_pack != null) throw new InvalidOperationException("Pack can be set only once for a quest");
                _pack = value;
            }
        }
    }
}