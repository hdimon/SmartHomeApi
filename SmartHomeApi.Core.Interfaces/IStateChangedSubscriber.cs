using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateChangedSubscriber
    {
        Task Notify(StateChangedEvent args);
    }
}