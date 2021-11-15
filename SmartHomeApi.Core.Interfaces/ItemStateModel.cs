using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public class ItemStateModel : IItemStateModel
    {
        public string ItemId { get; }
        public string ItemType { get; }

        public Dictionary<string, object> States { get; set; } = new Dictionary<string, object>();

        public ItemStateModel(string itemId, string itemType)
        {
            ItemId = itemId;
            ItemType = itemType;
        }
    }
}