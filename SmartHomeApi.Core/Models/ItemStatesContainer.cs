using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Models
{
    public class ItemStatesContainer : IStatesContainer
    {
        public Dictionary<string, IItemState> States { get; set; } = new Dictionary<string, IItemState>();

        public object Clone()
        {
            var clone = new ItemStatesContainer();

            foreach (var itemState in States)
            {
                clone.States.Add(itemState.Key, (IItemState)itemState.Value.Clone());
            }

            return clone;
        }
    }
}