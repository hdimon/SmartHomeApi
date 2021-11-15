using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemStateModel
    {
        string ItemId { get; }
        string ItemType { get; }

        Dictionary<string, object> States { get; set; }
    }
}