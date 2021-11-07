using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.ApiItemsLocatorTestStubs
{
    class TestItem : StandardItem
    {
        public TestItem(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager, helpersFabric,
            config)
        {
        }
    }
}