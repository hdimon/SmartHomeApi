using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.ItemsLocatorsBridges
{
    class DeprecatedItemsLocatorBridge : StandardItemsLocatorBridge
    {
        public DeprecatedItemsLocatorBridge(IItemsLocator locator) : base(locator)
        {
        }

        public override async Task ConfigAdded(IItemConfig config)
        {
            //Old items locators are not able to process this event
        }

        public override async Task ConfigUpdated(IItemConfig config)
        {
            //Old items locators are not able to process this event
        }

        public override async Task ConfigDeleted(string itemId)
        {
            //Old items locators are not able to process this event
        }
    }
}