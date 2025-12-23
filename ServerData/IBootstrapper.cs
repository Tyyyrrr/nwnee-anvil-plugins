using ServerData.SQLSchema;

namespace ServerData
{
    public interface IBootstrapper
    {
        IPlayerSQLMap GetPlayerSQLMap();
    }
}