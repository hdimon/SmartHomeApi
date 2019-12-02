using SmartHomeApi.Core.Interfaces;

namespace EventsPostgreSqlStorage
{
    public class StorageConfig : ItemConfigAbstract
    {
        public string ConnectionString { get; set; }

        public StorageConfig(string itemId, string itemType) : base(itemId, itemType)
        {
        }
    }
}