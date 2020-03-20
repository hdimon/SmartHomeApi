using System.Collections.Generic;
using System.Linq;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core.Services
{
    public class UncachedStatesProcessor : IUncachedStatesProcessor
    {
        private readonly ISmartHomeApiFabric _fabric;

        public UncachedStatesProcessor(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public void AddUncachedItemsFromConfig(ApiManagerStateContainer stateContainer)
        {
            var uncachedItems = _fabric.GetConfiguration().UncachedItems;

            foreach (var uncachedItem in uncachedItems)
            {
                if (string.IsNullOrWhiteSpace(uncachedItem.ItemId) ||
                    stateContainer.UncachedStates.ContainsKey(uncachedItem.ItemId))
                    continue;

                if (uncachedItem.ApplyOnlyEnumeratedStates && uncachedItem.States == null)
                    uncachedItem.States = new List<string>();

                stateContainer.UncachedStates.Add(uncachedItem.ItemId, uncachedItem);
            }
        }

        public void AddUncachedStatesFromItem(IStateGettable item, ApiManagerStateContainer stateContainer)
        {
            if (item.UncachedFields == null || !item.UncachedFields.Any())
                return;

            if (stateContainer.UncachedStates.ContainsKey(item.ItemId))
            {
                var uncachedItem = stateContainer.UncachedStates[item.ItemId];

                //If not ApplyOnlyEnumeratedStates then whole Item is uncached if ApplyOnlyEnumeratedStates then merge states
                if (uncachedItem.ApplyOnlyEnumeratedStates)
                {
                    foreach (var itemUncachedField in item.UncachedFields)
                    {
                        if (!uncachedItem.States.Contains(itemUncachedField))
                            uncachedItem.States.Add(itemUncachedField);
                    }
                }
            }
            else
                stateContainer.UncachedStates.Add(item.ItemId,
                    new AppSettingItemInfo
                    {
                        ItemId = item.ItemId,
                        ApplyOnlyEnumeratedStates = true,
                        States = item.UncachedFields.ToList()
                    });
        }

        public IItemState FilterOutUncachedStates(IItemState itemState, ApiManagerStateContainer stateContainer)
        {
            if (!stateContainer.UncachedStates.ContainsKey(itemState.ItemId))
                return itemState;

            var state = (IItemState)itemState.Clone();

            var uncachedState = stateContainer.UncachedStates[itemState.ItemId];

            //If ApplyOnlyEnumeratedStates = false then whole Item in uncached
            if (!uncachedState.ApplyOnlyEnumeratedStates)
                return null;

            var cachedStates = new Dictionary<string, object>();

            foreach (var stateState in state.States)
            {
                if (uncachedState.States.Contains(stateState.Key))
                    continue;

                cachedStates.Add(stateState.Key, stateState.Value);
            }

            state.States = cachedStates;

            return state;
        }
    }
}