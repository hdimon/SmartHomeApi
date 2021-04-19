using System;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace StandardTestPlugin1
{
    class StandardTestPlugin1Locator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType { get; } = "StandardTestPlugin1";
        public override Type ConfigType { get; } = typeof(StandardTestPlugin1Config);

        public StandardTestPlugin1Locator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            return new StandardTestPlugin1Main(Fabric.GetApiManager(),
                Fabric.GetItemHelpersFabric(config.ItemId, config.ItemType), config);
        }
    }
}