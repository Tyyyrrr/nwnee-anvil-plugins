namespace ServerData.SQLSchema
{
    public interface IQuestSQLMap : ISQLMap
    {
        public string QuestTag {get;}
        public string StageID {get;}
        public string IsCompleted {get;}
        public string Snapshot {get;}
    }
}