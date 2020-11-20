namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateGettable
    {
        string ItemId { get; }
        string ItemType { get; }
        IItemState GetState();
    }
}