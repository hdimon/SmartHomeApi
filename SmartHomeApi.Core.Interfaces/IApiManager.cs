using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiManager : IInitializable, IDisposable, IStateChangedNotifier
    {
        Task<ISetValueResult> SetValue(string itemId, string parameter, string value);
        Task<ISetValueResult> Increase(string itemId, string parameter);
        Task<ISetValueResult> Decrease(string itemId, string parameter);
        Task<IStatesContainer> GetState(bool transform = false);
        Task<IItemState> GetState(string itemId, bool transform = false);
        Task<object> GetState(string itemId, string parameter, bool transform = false);
        Task<IList<IItem>> GetItems();
        Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command);
    }
}