using System;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceManager : IInitializable, IDisposable, IStateChangedNotifier
    {
        Task SetValue(string deviceId, string parameter, string value);
        Task Increase(string deviceId, string parameter);
        Task Decrease(string deviceId, string parameter);
        IDeviceStatesContainer GetState();
        IDeviceState GetState(string deviceId);
        object GetState(string deviceId, string parameter);
    }
}