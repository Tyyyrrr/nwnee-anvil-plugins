using ServerData.SQLSchema;

namespace ServerData
{
    public interface IBootstrapper
    {
        IPlayerSQLMap GetPlayerSQLMap();
        ICreatureInspector GetCreatureInspector();
        ICustomClassesMap GetCustomClassesMap();
        ICustomFeatsMap GetCustomFeatsMap();
    }
}