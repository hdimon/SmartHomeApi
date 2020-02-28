using System;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace Mega2560ControllerDevice
{
    public class Mega2560ControllerLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "Mega2560Controller";
        public override Type ConfigType => typeof(Mega2560ControllerConfig);

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