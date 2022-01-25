using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ItemsConfigsLocatorStub : IItemsConfigLocator
    {
        public List<IItemConfig> Configs { get; } = new List<IItemConfig>();

        public bool IsInitialized { get; private set; }

        public Task Initialize()
        {
            IsInitialized = true;

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task<List<IItemConfig>> GetItemsConfigs(string itemType)
        {
            return Task.FromResult(Configs.Where(c => c.ItemType == itemType).ToList());
        }
    }
}