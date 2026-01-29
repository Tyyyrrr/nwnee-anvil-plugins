namespace QuestEditor.Shared
{
    public interface IStateful
    {
        public int Counter { get; set; }
        public bool IsDirty { get; }
        public void RecursiveApply();
        public void Discard();
        public void RefreshIsDirty();
    }
}
