using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class DeviceStateStorageStubHelper : IItemStateStorageHelper
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
