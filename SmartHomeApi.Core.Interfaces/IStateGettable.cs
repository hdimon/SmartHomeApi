using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateGettable
    {
        string ItemId { get; }
        string ItemType { get; }
        IItemState GetState();
        IList<string> UncachedFields { get; }
        IList<string> UntrackedFields { get; }
    }
}