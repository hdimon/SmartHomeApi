using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IInitializable
    {
        Task Initialize();
    }
}