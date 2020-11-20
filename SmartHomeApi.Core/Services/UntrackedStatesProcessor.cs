using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class UntrackedStatesProcessor : IUntrackedStatesProcessor
    {
        private readonly ISmartHomeApiFabric _fabric;

        public UntrackedStatesProcessor(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public void AddUntrackedItemsFromConfig(ApiManagerStateContainer stateContainer)
        {
            var untrackedItems = _fabric.GetConfiguration().UntrackedItems;

            foreach (var untrackedItem in untrackedItems)
            {
                if (string.IsNullOrWhiteSpace(untrackedItem.ItemId) ||
                    stateContainer.UntrackedStates.ContainsKey(untrackedItem.ItemId))
                    continue;

                if (untrackedItem.ApplyOnlyEnumeratedStates && untrackedItem.States == null)
                    untrackedItem.States = new List<string>();

                stateContainer.UntrackedStates.Add(untrackedItem.ItemId, untrackedItem);
            }
        }
    }
}