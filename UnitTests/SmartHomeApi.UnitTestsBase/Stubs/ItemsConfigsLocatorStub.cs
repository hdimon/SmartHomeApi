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

        public async Task Initialize()
        {
            IsInitialized = true;
        }

        public void Dispose()
        {
        }

        public async Task<List<IItemConfig>> GetItemsConfigs(string itemType)
        {
            return Configs.Where(c => c.ItemType == itemType).ToList();
        }
    }
}