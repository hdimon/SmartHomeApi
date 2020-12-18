using System;
using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemState : ICloneable
    {
        string ItemId { get; }
        string ItemType { get; }

        Dictionary<string, object> States { get; set; }
    }
}