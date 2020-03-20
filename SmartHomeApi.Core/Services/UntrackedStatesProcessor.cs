using System.Collections.Generic;
using System.Linq;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

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

        public void AddUntrackedStatesFromItem(IStateGettable item, ApiManagerStateContainer stateContainer)
        {
            if (item.UntrackedFields == null || !item.UntrackedFields.Any())
                return;

            if (stateContainer.UntrackedStates.ContainsKey(item.ItemId))
            {
                var untrackedItem = stateContainer.UntrackedStates[item.ItemId];

                //If not ApplyOnlyEnumeratedStates then whole Item is untracked if ApplyOnlyEnumeratedStates then merge states
                if (untrackedItem.ApplyOnlyEnumeratedStates)
                {
                    foreach (var itemUntrackedField in item.UntrackedFields)
                    {
                        if (!untrackedItem.States.Contains(itemUntrackedField))
                            untrackedItem.States.Add(itemUntrackedField);
                    }
                }
            }
            else
                stateContainer.UntrackedStates.Add(item.ItemId,
                    new AppSettingItemInfo
                    {
                        ItemId = item.ItemId,
                        ApplyOnlyEnumeratedStates = true,
                        States = item.UntrackedFields.ToList()
                    });
        }
    }
}