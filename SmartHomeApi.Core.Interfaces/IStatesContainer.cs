using System;
using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStatesContainer : ICloneable
    {
        Dictionary<string, IItemState> States { get; set; }
    }
}