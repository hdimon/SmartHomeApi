namespace SmartHomeApi.Core.Interfaces
{
    public interface IUncachedStatesProcessor
    {
        void AddUncachedItemsFromConfig(ApiManagerStateContainer stateContainer);
        IItemState FilterOutUncachedStates(IItemState itemState, ApiManagerStateContainer stateContainer);
    }
}