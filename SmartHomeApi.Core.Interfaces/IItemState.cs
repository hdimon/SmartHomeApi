﻿using System;
using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemState : ICloneable
    {
        string ItemId { get; }
        string ItemType { get; }
        ConnectionStatus ConnectionStatus { get; set; }

        Dictionary<string, object> States { get; set; }
    }
}