using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IInitializable
    {
        bool IsInitialized { get; }
        Task Initialize();
    }
}