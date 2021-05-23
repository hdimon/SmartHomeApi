using System;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.TestItem1Plugin
{
    class TestItem1 : StandardItem
    {
        public Func<StateChangedEvent, Task> OnProcessNotification { get; set; }

        public TestItem1(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager, helpersFabric,
            config)
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {
            await OnProcessNotification(args);
        }
    }
}