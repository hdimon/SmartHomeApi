using System;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace PluginWithLocatorConstructorTimeout
{
    class PluginWithLocatorConstructorTimeoutLocator : AutoRefreshItemsLocatorAbstract
    {
        public override string ItemType { get; } = "PluginWithLocatorConstructorTimeoutLocator";
        public override Type ConfigType { get; } = typeof(PluginWithLocatorConstructorTimeoutConfig);

        public PluginWithLocatorConstructorTimeoutLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
            Task.Delay(10000).Wait();
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            return new PluginWithLocatorConstructorTimeoutMain(Fabric.GetApiManager(),
                Fabric.GetItemHelpersFabric(config.ItemId, config.ItemType), config);
        }
    }
}