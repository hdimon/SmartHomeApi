using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ItemStateStorageStubHelper : IItemStateStorageHelper
    {
        public Task SaveState(object state, string fileNamePattern)
        {
            return Task.CompletedTask;
        }

        public T RestoreState<T>(string fileNamePattern)
        {
            return default;
        }
    }
}
