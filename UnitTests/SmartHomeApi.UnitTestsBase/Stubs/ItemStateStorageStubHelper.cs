using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ItemStateStorageStubHelper : IItemStateStorageHelper
    {
        public async Task SaveState(object state, string fileNamePattern)
        {
            
        }

        public T RestoreState<T>(string fileNamePattern)
        {
            return default;
        }
    }
}
