using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiManager : IInitializable, IDisposable, IStateChangedNotifier
    {
        Task<ISetValueResult> SetValue(string deviceId, string parameter, string value);
        Task<ISetValueResult> Increase(string deviceId, string parameter);
        Task<ISetValueResult> Decrease(string deviceId, string parameter);
        Task<IStatesContainer> GetState(bool transform = false);
        Task<IItemState> GetState(string deviceId, bool transform = false);
        Task<object> GetState(string deviceId, string parameter, bool transform = false);
        Task<IList<IItem>> GetItems();
        Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command);
    }
}