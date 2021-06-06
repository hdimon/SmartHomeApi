using System;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.StandardItemsLocator1Plugin
{
    class StandardItemsLocator1 : StandardItemsLocatorAbstract
    {
        public override string ItemType => nameof(StandardItemsLocator1Plugin);
        public override Type ConfigType => typeof(StandardItemsLocator1Config);

        public StandardItemsLocator1(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            return new StandardItemsLocator1Item(Fabric.GetApiManager(),
                Fabric.GetItemHelpersFabric(config.ItemId, config.ItemType), config);
        }
    }
}
