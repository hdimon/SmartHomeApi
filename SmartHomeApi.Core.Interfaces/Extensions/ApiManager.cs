using System.Threading.Tasks;
using Common.Utils;

namespace SmartHomeApi.Core.Interfaces.Extensions
{
    public static class ApiManager
    {
        public static async Task<T> GetState<T>(this IApiManager apiManager, string itemId, string parameter)
        {
            var state = await apiManager.GetState(itemId, parameter).ConfigureAwait(false);

            return TypeHelper.GetValue<T>(state);
        }

        public static async Task<T> GetState<T>(this IApiManager apiManager, string itemId, string parameter, T defaultValue)
        {
            var state = await apiManager.GetState(itemId, parameter).ConfigureAwait(false);

            return TypeHelper.GetValue(state, defaultValue);
        }
    }
}