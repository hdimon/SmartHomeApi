namespace SmartHomeApi.Core.Interfaces
{
    public interface IUncachedStatesProcessor
    {
        void AddUncachedItemsFromConfig(ApiManagerStateContainer stateContainer);
        void AddUncachedStatesFromItem(IStateGettable item, ApiManagerStateContainer stateContainer);
        IItemState FilterOutUncachedStates(IItemState itemState, ApiManagerStateContainer stateContainer);
    }
}