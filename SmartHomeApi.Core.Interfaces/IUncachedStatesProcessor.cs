namespace SmartHomeApi.Core.Interfaces
{
    public interface IUncachedStatesProcessor
    {
        IStatesContainer FilterOutUncachedStates(IStatesContainer state);
    }
}