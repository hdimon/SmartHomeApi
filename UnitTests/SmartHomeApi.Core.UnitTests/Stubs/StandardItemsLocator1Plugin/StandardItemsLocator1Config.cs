using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.StandardItemsLocator1Plugin
{
    class StandardItemsLocator1Config : ItemConfigAbstract
    {
        public string TestField { get; set; }

        public StandardItemsLocator1Config(string itemId, string itemType) : base(itemId, itemType)
        {
        }
    }
}