using SmartHomeApi.Core.Interfaces;

namespace EventsPostgreSqlStorage
{
    public class StorageConfig : ItemConfigAbstract
    {
        public StorageConfig(string itemId, string itemType) : base(itemId, itemType)
        {
        }
    }
}