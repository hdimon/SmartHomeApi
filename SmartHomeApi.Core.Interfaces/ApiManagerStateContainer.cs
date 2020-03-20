using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core.Interfaces
{
    public class ApiManagerStateContainer
    {
        public IStatesContainer State { get; set; }

        public Dictionary<string, AppSettingItemInfo> UntrackedStates { get; set; } =
            new Dictionary<string, AppSettingItemInfo>();

        public Dictionary<string, AppSettingItemInfo> UncachedStates { get; set; } =
            new Dictionary<string, AppSettingItemInfo>();

        public ApiManagerStateContainer(IStatesContainer state)
        {
            State = state;
        }
    }
}