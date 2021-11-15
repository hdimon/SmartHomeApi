using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiManager : IInitializable, IAsyncDisposable, IStateChangedNotifier
    {
        Task<ISetValueResult> SetValue(string itemId, string parameter, object value);
        Task<IStatesContainer> GetState();
        Task<IItemStateModel> GetState(string itemId);
        Task<object> GetState(string itemId, string parameter);
        Task<IList<IItem>> GetItems();
        Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command);
    }
}