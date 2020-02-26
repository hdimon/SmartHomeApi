using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace VirtualAlarmClockDevice
{
    public class VirtualAlarmClockLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "VirtualAlarmClockDevice";

        public override bool ImmediateInitialization => true;

        public VirtualAlarmClockLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            var helpersFabric = Fabric.GetItemHelpersFabric();

            return new VirtualAlarmClock(helpersFabric, config);
        }
    }
}