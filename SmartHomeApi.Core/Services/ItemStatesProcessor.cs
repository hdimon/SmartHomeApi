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
        private readonly ConcurrentDictionary<string, IItemState> _states = new ConcurrentDictionary<string, IItemState>();

        public ItemStatesProcessor(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public IItemState GetOrCreateItemState(string itemId, string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentNullException();

            return _states.GetOrAdd(itemId, key => ItemStateFactory(key, itemType));
        }

        private IItemState ItemStateFactory(string itemId, string itemType)
        {
            var proxy = new ItemStateProxy(itemId, itemType, _fabric, new ConcurrentDictionary<string, object>());

            return new ItemState(itemId, itemType, proxy);
        }

        public IStatesContainer GetStatesContainer()
        {
            var container = new ItemStatesContainer();

            foreach (var (itemId, states) in _states)
            {
                var model = new ItemStateModel(itemId, states.ItemType);
                model.States = new Dictionary<string, object>(states.GetStates());
                container.States.Add(itemId, model);
            }

            return container;
        }

        public IItemStateModel GetItemState(string itemId)
        {
            if (!_states.TryGetValue(itemId, out var state) || state == null)
                return null;

            var model = new ItemStateModel(itemId, state.ItemType);
            model.States = new Dictionary<string, object>(state.GetStates());

            return model;
        }

        public object GetItemState(string itemId, string parameter)
        {
            if (!_states.TryGetValue(itemId, out var state) || state == null)
                return null;

            return state.GetState(parameter);
        }
    }
}