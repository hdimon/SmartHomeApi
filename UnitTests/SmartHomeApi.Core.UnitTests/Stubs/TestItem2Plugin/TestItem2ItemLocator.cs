using System;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.TestItem2Plugin
{
    class TestItem2ItemLocator : StandardItemsLocatorAbstract
    {
        public override string ItemType { get; } = "TestItem2";
        public override Type ConfigType { get; } = typeof(TestItem2Config);

        public Func<IItemConfig, Task> OnConfigAdded;
        public Func<IItemConfig, Task> OnConfigUpdated;
        public Func<string, Task> OnConfigDeleted;

        public TestItem2ItemLocator(ISmartHomeApiFabric fabric) : base(fabric)
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