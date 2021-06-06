using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.StandardItemsLocator1Plugin
{
    class StandardItemsLocator1Item : StandardItem
    {
        public StandardItemsLocator1Item(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(
            manager, helpersFabric, config)
        {
        }
    }
}