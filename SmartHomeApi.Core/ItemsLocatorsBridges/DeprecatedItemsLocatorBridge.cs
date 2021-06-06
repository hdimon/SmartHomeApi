using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.ItemsLocatorsBridges
{
    class DeprecatedItemsLocatorBridge : StandardItemsLocatorBridge
    {
        public override bool IsInitialized
        {
            get
            {
                if (_locator is IInitializable initializable)
                    return initializable.IsInitialized;

                //If items locator does not implement IInitializable then take it like not initialized
                return false;
            }
        }

        public DeprecatedItemsLocatorBridge(IItemsLocator locator) : base(locator)
        {
        }

        public override async Task Initialize()
        {
            if (_locator is IInitializable initializable)
                await initializable.Initialize();
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