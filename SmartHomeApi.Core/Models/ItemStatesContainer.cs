using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Models
{
    public class ItemStatesContainer : IStatesContainer
    {
        public Dictionary<string, IItemStateModel> States { get; set; } = new Dictionary<string, IItemStateModel>();
    }
}