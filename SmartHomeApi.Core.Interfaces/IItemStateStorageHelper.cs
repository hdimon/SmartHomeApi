using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemStateStorageHelper
    {
        Task SaveState(object state, string fileNamePattern);
        T RestoreState<T>(string fileNamePattern);
    }
}