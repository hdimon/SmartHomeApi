using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Models
{
    public class DeviceStatesContainer : IStatesContainer
    {
        public Dictionary<string, IItemState> States { get; set; } = new Dictionary<string, IItemState>();
    }
}