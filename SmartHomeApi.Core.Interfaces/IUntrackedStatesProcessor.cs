namespace SmartHomeApi.Core.Interfaces
{
    public interface IUntrackedStatesProcessor
    {
        void AddUntrackedItemsFromConfig(ApiManagerStateContainer stateContainer);
        void AddUntrackedStatesFromItem(IStateGettable item, ApiManagerStateContainer stateContainer);
    }
}