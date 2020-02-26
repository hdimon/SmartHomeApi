using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace BreezartLux550Device
{
    public class BreezartLux550Locator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "BreezartLux550";

        public BreezartLux550Locator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            var helpersFabric = Fabric.GetItemHelpersFabric();

            return new BreezartLux550(helpersFabric, config);
        }
    }
}