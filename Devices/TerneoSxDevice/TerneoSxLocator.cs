using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace TerneoSxDevice
{
    public class TerneoSxLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType => "TerneoSx";

        public TerneoSxLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            var helpersFabric = Fabric.GetItemHelpersFabric();

            return new TerneoSx(helpersFabric, config);
        }
    }
}