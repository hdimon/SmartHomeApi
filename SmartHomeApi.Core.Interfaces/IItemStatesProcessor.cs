namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemStatesProcessor
    {
        IItemState GetOrCreateItemState(string itemId, string itemType);
        /// <summary>
        /// Returns copy of actual item states.
        /// </summary>
        /// <returns></returns>
        IStatesContainer GetStatesContainer();
        /// <summary>
        /// Returns copy of actual item state or null if no state.
        /// </summary>
        /// <returns></returns>
        IItemStateModel GetItemState(string itemId);
        /// <summary>
        /// Returns value of item state by parameter or null if no state with parameter provided.
        /// </summary>
        /// <returns></returns>
        object GetItemState(string itemId, string parameter);
    }
}