using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStatesContainer
    {
        Dictionary<string, IItemState> States { get; set; }
    }
}