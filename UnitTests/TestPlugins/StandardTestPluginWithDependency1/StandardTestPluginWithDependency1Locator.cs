using System;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace StandardTestPluginWithDependency1
{
    class StandardTestPluginWithDependency1Locator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType { get; } = "StandardTestPluginWithDependency1";
        public override Type ConfigType { get; } = typeof(StandardTestPluginWithDependency1Config);

        public StandardTestPluginWithDependency1Locator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            return new StandardTestPluginWithDependency1Main(Fabric.GetApiManager(),
                Fabric.GetItemHelpersFabric(config.ItemId, config.ItemType), config);
        }
    }
}