using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceStateStorageHelper
    {
        Task SaveState(object state, string fileNamePattern);
        T RestoreState<T>(string fileNamePattern);
    }
}