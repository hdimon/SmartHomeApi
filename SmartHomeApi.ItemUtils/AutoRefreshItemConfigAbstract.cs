namespace SmartHomeApi.ItemUtils
{
    public class AutoRefreshItemConfigAbstract : ItemConfigAbstract
    {
        public int RequestDataIntervalMS { get; set; }

        public AutoRefreshItemConfigAbstract(string itemId, string itemType) : base(itemId, itemType)
        {
        }
    }
}