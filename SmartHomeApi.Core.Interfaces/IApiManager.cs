using System;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiManager : IInitializable, IDisposable, IStateChangedNotifier
    {
        Task<ISetValueResult> SetValue(string deviceId, string parameter, string value);
        Task<ISetValueResult> Increase(string deviceId, string parameter);
        Task<ISetValueResult> Decrease(string deviceId, string parameter);
        IStatesContainer GetState();
        IItemState GetState(string deviceId);
        object GetState(string deviceId, string parameter);
    }
}