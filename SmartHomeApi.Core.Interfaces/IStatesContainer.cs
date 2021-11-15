using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStatesContainer
    {
        Dictionary<string, IItemStateModel> States { get; set; }
    }
}