using System.Collections.Generic;
using System.Linq;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class UncachedStatesProcessor : IUncachedStatesProcessor
    {
        private readonly ISmartHomeApiFabric _fabric;

        public UncachedStatesProcessor(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public IStatesContainer FilterOutUncachedStates(IStatesContainer state)
        {
            var uncachedItems = _fabric.GetConfiguration().UncachedItems;

            if (!uncachedItems.Any())
                return state;

            var itemsToRemove = new List<string>();

            foreach (var (itemId, itemState) in state.States)
            {
                var uncachedItem = uncachedItems.FirstOrDefault(i => i.ItemId == itemId);

                if (uncachedItem == null)
                {
                    continue;
                }

                if (!uncachedItem.ApplyOnlyEnumeratedStates) //Skip whole item
                {
                    itemsToRemove.Add(itemId);
                    continue;
                }

                var parametersToRemove = new List<string>();

                foreach (var (key, value) in itemState.States)
                {
                    if (uncachedItem.States.Contains(key))
                        parametersToRemove.Add(key);
                }

                foreach (var key in parametersToRemove)
                {
                    itemState.States.Remove(key);
                }
            }

            foreach (var itemId in itemsToRemove)
            {
                state.States.Remove(itemId);
            }

            return state;
        }
    }
}