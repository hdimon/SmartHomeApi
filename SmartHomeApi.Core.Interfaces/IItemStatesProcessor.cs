namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemStatesProcessor
    {
        IItemStateNew GetOrCreateItemState(string itemId, string itemType);
        IStatesContainer GetStatesContainer();
    }
}