using System;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.TestItem1Plugin
{
    class TestItem1ItemLocator : StandardItemsLocatorAbstract
    {
        public override string ItemType { get; } = "TestItem1";
        public override Type ConfigType { get; } = typeof(TestItem1Config);

        public Func<IItemConfig, Task> OnConfigAdded;
        public Func<IItemConfig, Task> OnConfigUpdated;
        public Func<string, Task> OnConfigDeleted;

        public TestItem1ItemLocator(ISmartHomeApiFabric fabric) : base(fabric)
        {
        }

        public override async Task ConfigAdded(IItemConfig config)
        {
            await base.ConfigAdded(config);

            await OnConfigAdded(config);
        }

        public override async Task ConfigUpdated(IItemConfig config)
        {
            await base.ConfigUpdated(config);

            await OnConfigUpdated(config);
        }

        public override async Task ConfigDeleted(string itemId)
        {
            await base.ConfigDeleted(itemId);

            await OnConfigDeleted(itemId);
        }

        protected override IItem ItemFactory(IItemConfig config)
        {
            throw new NotImplementedException();
        }
    }
}