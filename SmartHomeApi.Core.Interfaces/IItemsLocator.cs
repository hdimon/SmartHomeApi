using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsLocator
    {
        string ItemType { get; }
        Type ConfigType { get; }
        bool ImmediateInitialization { get; }
        Task<IEnumerable<IItem>> GetItems();
    }
}