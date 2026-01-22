namespace ServerData.SQLSchema
{
    public interface ISQLMap
    {
        string TableName {get;}
        string CreateTableIfNotExistsQuery {get;}
    }
}