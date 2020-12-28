namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemStatesProcessor
    {
        IItemStateNew GetOrCreateItemState(string itemId, string itemType);
        /// <summary>
        /// Returns copy of actual item states.
        /// </summary>
        /// <returns></returns>
        IStatesContainer GetStatesContainer();
    }
}