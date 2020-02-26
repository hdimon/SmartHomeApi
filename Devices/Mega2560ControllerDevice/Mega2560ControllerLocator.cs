using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace Mega2560ControllerDevice
{
    public class Mega2560ControllerLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "Mega2560Controller";

        public Mega2560ControllerLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            var helpersFabric = Fabric.GetItemHelpersFabric();

            return new Mega2560Controller(helpersFabric, config);
        }
    }
}