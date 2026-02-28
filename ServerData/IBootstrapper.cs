using ServerData.SQLSchema;

namespace ServerData
{
    public interface IBootstrapper
    {
        IPlayerSQLMap GetPlayerSQLMap();
        ICreatureInspector GetCreatureInspector();
        ICustomClassesMap GetCustomClassesMap();
        ICustomFeatsMap GetCustomFeatsMap();
        ICustomBaseItemTypesMap GetCustomBaseItemTypesMap();
        IBodyAppearanceProvider GetBodyAppearanceProvider();
        IItemAppearanceProvider GetItemAppearanceProvider();
        IBodyAppearanceSQLMap GetBodyAppearanceSQLMap();
        IIdentitySQLMap GetIdentitySQLMap();
        IAcquaintanceSQLMap GetAcquaintanceSQLMap();
        IQuestSQLMap GetQuestSQLMap();
        IQuestObjectiveSQLMap GetQuestObjectiveSQLMap();
        IQuestVisibilitySQLMap GetQuestVisibilitySQLMap();
    }
}