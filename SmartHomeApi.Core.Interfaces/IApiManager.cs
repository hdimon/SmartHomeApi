using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiManager : IInitializable, IAsyncDisposable, IStateChangedNotifier
    {
        Task<ISetValueResult> SetValue(string itemId, string parameter, object value);
        Task<IStatesContainer> GetState();
        Task<IItemStateModel> GetState(string itemId);
        Task<object> GetState(string itemId, string parameter);
        Task<IList<IItem>> GetItems();

        /// <summary>
        /// Invoke plugin's methods which are marked with ExecutableAttribute.
        /// </summary>
        /// <param name="itemId">ItemId</param>
        /// <param name="command">Name of command (name of marked method)</param>
        /// <param name="data">Data which is input parameter for marked method</param>
        /// <param name="resultType">Optional parameter: set it if you want output data to be mapped to your type</param>
        /// <returns></returns>
        Task<object> Execute(string itemId, string command, object data, Type resultType = null);
    }
}