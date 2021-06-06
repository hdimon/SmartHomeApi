namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateGettable : IItem
    {
        IItemState GetState();
    }
}