using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Models;

namespace SmartHomeApi.Core.Services
{
    public class ItemStatesProcessor : IItemStatesProcessor
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly ConcurrentDictionary<string, IItemStateNew> _states = new ConcurrentDictionary<string, IItemStateNew>();

        public ItemStatesProcessor(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public IItemStateNew GetOrCreateItemState(string itemId, string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentNullException();

            return _states.GetOrAdd(itemId, key => ItemStateFactory(key, itemType));
        }

        private IItemStateNew ItemStateFactory(string itemId, string itemType)
        {
            var proxy = new ItemStateProxy(itemId, itemType, _fabric, new ConcurrentDictionary<string, object>());

            return new ItemStateNew(itemId, itemType, proxy);
        }

        public IStatesContainer GetStatesContainer()
        {
            var container = new ItemStatesContainer();

            foreach (var (itemId, states) in _states)
            {
                var itemState = new ItemState(itemId, states.ItemType);
                itemState.States = new Dictionary<string, object>(states.GetStates());
                container.States.Add(itemId, itemState);
            }

            return container;
        }
    }
}