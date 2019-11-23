using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateChangedSubscriber : IItem
    {
        Task Notify(StateChangedEvent args);
    }
}