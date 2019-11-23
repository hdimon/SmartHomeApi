using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemState
    {
        string ItemId { get; }
        string ItemType { get; }
        ConnectionStatus ConnectionStatus { get; set; }

        Dictionary<string, object> States { get; set; }
    }
}