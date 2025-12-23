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
        

        static DataProviders()
        {
            var bootstrapper = GetBootstrapper();

            PlayerSQLMap = bootstrapper.GetPlayerSQLMap();
            CreatureInspector = bootstrapper.GetCreatureInspector();
            CustomClassesMap = bootstrapper.GetCustomClassesMap();
            CustomFeatsMap = bootstrapper.GetCustomFeatsMap();
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
    }
}