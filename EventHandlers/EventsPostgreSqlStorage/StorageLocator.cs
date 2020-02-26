using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace EventsPostgreSqlStorage
{
    public class StorageLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "EventsPostgreSqlStorage";

        public override bool ImmediateInitialization => true;

        public StorageLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            var helpersFabric = Fabric.GetItemHelpersFabric();
            var manager = Fabric.GetApiManager();

            return new Storage(manager, helpersFabric, config);
        }
    }
}