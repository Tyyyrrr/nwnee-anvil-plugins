namespace ServerData.SQLSchema
{
    public interface IPlayerSQLMap : ISQLMap
    {
        public string Character {get;}
        public string UUID {get;}
    }
}