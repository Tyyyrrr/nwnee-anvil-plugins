using System;
using System.Linq;
using System.Reflection;
using ServerData.SQLSchema;

namespace ServerData
{
    public static class DataProviders
    {
        private static IBootstrapper GetBootstrapper()
        {
            var asm = Assembly.GetAssembly(typeof(DataProviders)) ?? throw new InvalidOperationException("Failed to get the assembly");
            var type = asm.GetTypes().FirstOrDefault(t=>!t.IsInterface && t.IsAssignableTo(typeof(IBootstrapper))) ?? throw new InvalidOperationException("Bootstrapper is not implemented");
            var bootstrapper = (Activator.CreateInstance(type) as IBootstrapper) ?? throw new InvalidOperationException("Failed to create concrete Bootstrapper instance");
            return bootstrapper;
        }
        
        static DataProviders() => Bootstrap();
        static bool bootstrapped = false;
        public static void Bootstrap()
        {
            if (bootstrapped)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Already bootstrapped");
                return;
            }

            bootstrapped = true;

            var bootstrapper = GetBootstrapper();

            // Core:
            PlayerSQLMap = bootstrapper.GetPlayerSQLMap();
            CreatureInspector = bootstrapper.GetCreatureInspector();
            CustomClassesMap = bootstrapper.GetCustomClassesMap();
            CustomFeatsMap = bootstrapper.GetCustomFeatsMap();

            // Appearance editor:
            CustomBaseItemTypesMap = bootstrapper.GetCustomBaseItemTypesMap();
            BodyAppearanceProvider = bootstrapper.GetBodyAppearanceProvider();
            ItemAppearanceProvider = bootstrapper.GetItemAppearanceProvider();
            BodyAppearanceSQLMap = bootstrapper.GetBodyAppearanceSQLMap();

            // False identities system:
            IdentitySQLMap = bootstrapper.GetIdentitySQLMap();
            AcquaintanceSQLMap = bootstrapper.GetAcquaintanceSQLMap();

            // Quest system:
            QuestSQLMap = bootstrapper.GetQuestSQLMap();
            QuestObjectiveSQLMap = bootstrapper.GetQuestObjectiveSQLMap();
            QuestVisibilitySQLMap = bootstrapper.GetQuestVisibilitySQLMap();

            // Behavior trees:
            BehaviorTreesProvider = bootstrapper.GetBehaviorTreesProvider();
        }


        public static IPlayerSQLMap PlayerSQLMap
        {
            get => _playerSQLMap ?? throw new InvalidOperationException($"{nameof(IPlayerSQLMap)} is not initialized.");
            set
            {
                if(_playerSQLMap != null) throw new InvalidOperationException($"{nameof(IPlayerSQLMap)} is already initialized.");
                _playerSQLMap = value;
            } 
        } static IPlayerSQLMap? _playerSQLMap;


        public static ICreatureInspector CreatureInspector
        {
            get => _creatureInspector ?? throw new InvalidOperationException($"{nameof(ICreatureInspector)} is not initialized.");
            set
            {
                if(_creatureInspector != null) throw new InvalidOperationException($"{nameof(ICreatureInspector)} is already initialized.");
                _creatureInspector = value;
            }
        } static ICreatureInspector? _creatureInspector;
    
    
        public static ICustomClassesMap CustomClassesMap
        {
            get => _customClassesMap ?? throw new InvalidOperationException($"{nameof(ICustomClassesMap)} is not initialized.");
            set
            {
                if(_customClassesMap != null) throw new InvalidOperationException($"{nameof(ICustomClassesMap)} is already initialized.");
                _customClassesMap = value;
            } 
        } static ICustomClassesMap? _customClassesMap;


        public static ICustomFeatsMap CustomFeatsMap
        {
            get => _customFeatsMap ?? throw new InvalidOperationException($"{nameof(ICustomFeatsMap)} is not initialized.");
            set
            {
                if(_customFeatsMap != null) throw new InvalidOperationException($"{nameof(ICustomFeatsMap)} is already initialized.");
                _customFeatsMap = value;
            } 
        } static ICustomFeatsMap? _customFeatsMap;


        public static ICustomBaseItemTypesMap CustomBaseItemTypesMap
        {
            get => _customBaseItemTypesMap ?? throw new InvalidOperationException($"{nameof(ICustomBaseItemTypesMap)} is not initialized.");
            set
            {
                if(_customBaseItemTypesMap != null) throw new InvalidOperationException($"{nameof(ICustomBaseItemTypesMap)} is already initialized.");
                _customBaseItemTypesMap = value;
            } 
        } static ICustomBaseItemTypesMap? _customBaseItemTypesMap;
    
    
        public static IBodyAppearanceProvider BodyAppearanceProvider
        {
            get => _bodyAppearanceProvider ?? throw new InvalidOperationException($"{nameof(IBodyAppearanceProvider)} is not initialized.");
            set
            {
                if(_bodyAppearanceProvider != null) throw new InvalidOperationException($"{nameof(IBodyAppearanceProvider)} is already initialized.");
                _bodyAppearanceProvider = value;
            }
        } static IBodyAppearanceProvider? _bodyAppearanceProvider;


        public static IItemAppearanceProvider ItemAppearanceProvider
        {
            get => _itemAppearanceProvider ?? throw new InvalidOperationException($"{nameof(IItemAppearanceProvider)} is not initialized.");
            set
            {
                if(_itemAppearanceProvider != null) throw new InvalidOperationException($"{nameof(IItemAppearanceProvider)} is already initialized.");
                _itemAppearanceProvider = value;
            }
        } static IItemAppearanceProvider? _itemAppearanceProvider;


        public static IBodyAppearanceSQLMap BodyAppearanceSQLMap
        {
            get => _bodyAppearanceSQLMap ?? throw new InvalidOperationException($"{nameof(IBodyAppearanceSQLMap)} is not initialized.");
            set
            {
                if(_bodyAppearanceSQLMap != null) throw new InvalidOperationException($"{nameof(IBodyAppearanceSQLMap)} is already initialized.");
                _bodyAppearanceSQLMap = value;
            } 
        } static IBodyAppearanceSQLMap? _bodyAppearanceSQLMap;


        public static IIdentitySQLMap IdentitySQLMap
        {
            get => _identitySQLMap ?? throw new InvalidOperationException($"{nameof(IIdentitySQLMap)} is not initialized.");
            set
            {
                if(_identitySQLMap != null) throw new InvalidOperationException($"{nameof(IIdentitySQLMap)} is already initialized.");
                _identitySQLMap = value;
            } 
        } static IIdentitySQLMap? _identitySQLMap;


        public static IAcquaintanceSQLMap AcquaintanceSQLMap
        {
            get => _acquaintanceSQLMap ?? throw new InvalidOperationException($"{nameof(IAcquaintanceSQLMap)} is not initialized.");
            set
            {
                if(_acquaintanceSQLMap != null) throw new InvalidOperationException($"{nameof(IAcquaintanceSQLMap)} is already initialized.");
                _acquaintanceSQLMap = value;
            } 
        } static IAcquaintanceSQLMap? _acquaintanceSQLMap;

        

        public static IQuestSQLMap QuestSQLMap
        {
            get => _questSQLMap ?? throw new InvalidOperationException($"{nameof(IQuestSQLMap)} is not initialized.");
            set
            {
                if(_questSQLMap != null) throw new InvalidOperationException($"{nameof(IQuestSQLMap)} is already initialized.");
                _questSQLMap = value;
            } 
        } static IQuestSQLMap? _questSQLMap;
        

        public static IQuestObjectiveSQLMap QuestObjectiveSQLMap
        {
            get => _questObjectiveSQLMap ?? throw new InvalidOperationException($"{nameof(IQuestObjectiveSQLMap)} is not initialized.");
            set
            {
                if(_questObjectiveSQLMap != null) throw new InvalidOperationException($"{nameof(IQuestObjectiveSQLMap)} is already initialized.");
                _questObjectiveSQLMap = value;
            } 
        } static IQuestObjectiveSQLMap? _questObjectiveSQLMap;

        public static IQuestVisibilitySQLMap QuestVisibilitySQLMap
        {
            get => _questVisibilitySQLMap ?? throw new InvalidOperationException($"{nameof(IQuestVisibilitySQLMap)} is not initialized.");
            set
            {
                if(_questVisibilitySQLMap != null) throw new InvalidOperationException($"{nameof(IQuestVisibilitySQLMap)} is already initialized.");
                _questVisibilitySQLMap = value;
            } 
        } static IQuestVisibilitySQLMap? _questVisibilitySQLMap;
        
        public static IBehaviorTreesProvider BehaviorTreesProvider
        {
            get => _behaviorTreesProvider ?? throw new InvalidOperationException($"{nameof(IBehaviorTreesProvider)} is not initialized.");
            set
            {
                if(_behaviorTreesProvider != null) throw new InvalidOperationException($"{nameof(IBehaviorTreesProvider)} is already initialized.");
                _behaviorTreesProvider = value;
            } 
        } static IBehaviorTreesProvider? _behaviorTreesProvider;
    }
}