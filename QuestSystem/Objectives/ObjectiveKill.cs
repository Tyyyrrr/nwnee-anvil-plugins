namespace QuestSystem.Objectives
{
    internal sealed class ObjectiveKill : Objective
    {
        private int _kills = 0;
        public override void Proceed() { _kills++; }
    }
}