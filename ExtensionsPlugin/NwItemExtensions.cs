using Anvil.API;

namespace ExtensionsPlugin
{
    public static class NwItemExtensions
    {
        public static bool IsShield(this NwItem item) => item.BaseItem.ItemType switch{
            BaseItemType.SmallShield or BaseItemType.LargeShield or BaseItemType.TowerShield => true,
            _ => false
        };
    }
}