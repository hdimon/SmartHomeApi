using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.DeviceUtils
{
    public class ItemConfigAbstract : IItemConfig
    {
        public string ItemId { get; }
        public string ItemType { get; }

        public ItemConfigAbstract(string itemId, string itemType)
        {
            ItemId = itemId;
            ItemType = itemType;
        }
    }
}