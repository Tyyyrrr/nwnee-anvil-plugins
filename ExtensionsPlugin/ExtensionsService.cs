using Anvil.Services;

using EasyConfig;

namespace ExtensionsPlugin
{
    [ServiceBinding(typeof(ExtensionsService))]
    internal sealed class ExtensionsService
    {
        public ExtensionsService(ConfigurationService configService)
        {
            NwCreatureExtensions.CacheConfig(configService);
        }
    }
}