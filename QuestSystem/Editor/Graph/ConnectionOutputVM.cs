namespace QuestEditor.Graph
{
    public class ConnectionOutputVM(int sourceID, int targetID) : ConnectionSocketVM(sourceID)
    {
        public int TargetID
        {
            get => _targetID;
            set => SetProperty(ref _targetID, value);
        } int _targetID = targetID;
    }
}
