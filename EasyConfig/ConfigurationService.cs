using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

using Anvil.Plugins;
using Anvil.Services;

using NLog;

namespace EasyConfig;

[ServiceBinding(typeof(ConfigurationService))]
public sealed class ConfigurationService(PluginManager pm, PluginStorageService ps)
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    private readonly PluginManager _pluginManager = pm;


    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };


    private readonly string _easyConfigStorageDirectory = ps.GetPluginStoragePath(typeof(ConfigurationService).Assembly);


    private readonly Dictionary<Type, IConfig> _cache = [];



    private string ResolveConfigPath(Type type)
    {
        if (!type.IsSealed) throw new InvalidOperationException($"{type.Name} must be sealed to implement {nameof(IConfig)}");

        if (type.GetCustomAttribute<ConfigFileAttribute>() is not ConfigFileAttribute attr)
            throw new InvalidOperationException($"{type.Name} must be decorated with {nameof(ConfigFileAttribute)} to implement {nameof(IConfig)}");

        if (string.IsNullOrEmpty(attr.FileName) || Path.EndsInDirectorySeparator(attr.FileName) || Path.HasExtension(attr.FileName))
            throw new InvalidOperationException($"{attr.FileName} is not valid config file name");

        if (!_pluginManager.IsPluginAssembly(type.Assembly))
            throw new InvalidOperationException($"{type.Assembly.GetName().FullName} is not a plugin assembly.");

        var pluginName = _pluginManager.GetPlugin(type.Assembly)?.Name.Name;

        if(string.IsNullOrEmpty(pluginName))
            throw new InvalidOperationException($"{type.Assembly.GetName().FullName} does not have associated plugin, or the plugin name is null or empty.");

        var dir = Path.Combine(_easyConfigStorageDirectory, pluginName);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return Path.Combine(dir, attr.FileName + ".cfg");
    }




    public T GetConfig<T>() where T : class, IConfig
    {
        if (_cache.TryGetValue(typeof(T), out var cachedCfg))
            return (T)cachedCfg;

        string path = ResolveConfigPath(typeof(T));

        var cfg = LoadOrCreateConfig<T>(path);

        _cache.Add(typeof(T), cfg);

        _log.Info($"Created configuration file at \'{path}\'");

        return cfg;
    }



    private static T LoadOrCreateConfig<T>(string filePath) where T : class, IConfig
    {
        string json;
        if (!File.Exists(filePath))
        {
            var ctor = typeof(T).GetConstructor(Type.EmptyTypes);

            if (ctor == null || !ctor.IsPublic)
                throw new InvalidOperationException($"Type {typeof(T).FullName} must have a public parameterless constructor.");

            if (Activator.CreateInstance<T>() is not T cfg)
                throw new InvalidOperationException($"Failed to initialize default configuration object of type {typeof(T).FullName}.");

            cfg.Coerce();

            if (!cfg.IsValid(out var err))
                throw new ConfigValidationException(filePath, err);
            
            json = JsonSerializer.Serialize(cfg, _jsonOptions);

            File.WriteAllText(filePath, json);

            return cfg;
        }

        var bytes = File.ReadAllBytes(filePath);

        var conf = JsonSerializer.Deserialize<T>(bytes, _jsonOptions) ?? throw new InvalidOperationException($"Deserialized configuration file \"{filePath}\" to \'null\'");

        conf.Coerce();

        if (!conf.IsValid(out var error))
            throw new ConfigValidationException(filePath, error);

        json = JsonSerializer.Serialize(conf, _jsonOptions);

        File.WriteAllText(filePath, json);

        return conf;
    }

    
    
}