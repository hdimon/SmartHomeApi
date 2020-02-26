using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace VirtualStateDevice
{
    public class VirtualStateLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "VirtualStateDevice";

        public override bool ImmediateInitialization => true;

        public VirtualStateLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            var helpersFabric = Fabric.GetItemHelpersFabric();

            return new VirtualState(helpersFabric, config);
        }
    }
}