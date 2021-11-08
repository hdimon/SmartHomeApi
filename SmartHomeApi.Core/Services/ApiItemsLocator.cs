﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;

namespace SmartHomeApi.Core.Services
{
    public class ApiItemsLocator : IApiItemsLocator
    {
        private bool _disposed;
        private readonly IApiLogger _logger;
        private readonly IItemsPluginsLocator _pluginsLocator;
        /// <summary>
        /// Process items only in single thread. It's ok because it's not supposed that items
        /// will be added or removed several times per second excepting startup.
        /// </summary>
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly Dictionary<string, IStandardItemsLocatorBridge> _itemsLocators = new();
        private readonly IList<IItem> _items = new List<IItem>();
        private ImmutableList<IItem> _immutableItems = ImmutableList<IItem>.Empty;

        public bool IsInitialized { get; private set; }

        public event EventHandler<ItemEventArgs> ItemAdded;
        public event EventHandler<ItemEventArgs> ItemDeleted;

        public ApiItemsLocator(ISmartHomeApiFabric fabric)
        {
            _logger = fabric.GetApiLogger();
            _pluginsLocator = fabric.GetItemsPluginsLocator();
        }

        public Task<IEnumerable<IItem>> GetItems()
        {
            return Task.FromResult<IEnumerable<IItem>>(_immutableItems);
        }

        public async Task Initialize()
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var items = new List<IItem>();
                var itemsLocators = (await _pluginsLocator.GetItemsLocators()).ToList();

                _pluginsLocator.ItemLocatorAddedOrUpdated += OnItemLocatorAddedOrUpdated;
                _pluginsLocator.BeforeItemLocatorDeleted += OnBeforeItemLocatorDeleted;

                //TODO sorting will be in GetItemsSortedByPriority method after introducing InitialLoadPriority config property
                //TODO Instead of ImmediateInitialization introduce InitialLoadPriority setting in Item config, group items by this and initialize them in groups
                itemsLocators = itemsLocators.OrderByDescending(p => p.ImmediateInitialization).ToList();

                _logger.Info("Start items locators initialization...");

                foreach (var locator in itemsLocators)
                {
                    _logger.Info($"Start {locator.ItemType} items locator initialization...");

                    await locator.Initialize();

                    _logger.Info($"{locator.ItemType} items locator has been initialized.");

                    _itemsLocators.Add(locator.ItemType, locator);

                    var locatorItems = await locator.GetItems();
                    locator.ItemAdded += OnItemAdded;
                    locator.ItemDeleted += OnItemDeleted;

                    items.AddRange(locatorItems);
                }

                _logger.Info("Items locators have been initialized.");

                var sortedItems = GetItemsSortedByPriority(items);

                _logger.Info("Start items initialization...");

                foreach (var item in sortedItems)
                {
                    await ProcessNewItem(item);
                }

                _logger.Info("Items have been initialized.");

                IsInitialized = true;
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private List<IItem> GetItemsSortedByPriority(List<IItem> items)
        {
            var sortedItems = new List<IItem>();
            sortedItems.AddRange(items);

            return sortedItems;
        }

        public virtual ValueTask DisposeAsync()
        {
            if (_disposed)
                return ValueTask.CompletedTask;

            _pluginsLocator.ItemLocatorAddedOrUpdated -= OnItemLocatorAddedOrUpdated;
            _pluginsLocator.BeforeItemLocatorDeleted -= OnBeforeItemLocatorDeleted;

            _logger.Info("ApiItemsLocator has been disposed.");

            _disposed = true;

            return ValueTask.CompletedTask;
        }

        private async void OnItemLocatorAddedOrUpdated(object sender, ItemLocatorEventArgs e)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var locators = await _pluginsLocator.GetItemsLocators();

                var itemsLocatorBridge = locators.First(p => p.ItemType == e.ItemType);

                await ProcessNewLocator(itemsLocatorBridge);
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task ProcessNewLocator(IStandardItemsLocatorBridge locator)
        {
            _logger.Info($"Start {locator.ItemType} items locator initialization...");

            await locator.Initialize();

            _logger.Info($"{locator.ItemType} items locator has been initialized.");

            _itemsLocators.Add(locator.ItemType, locator);

            var items = await locator.GetItems();

            locator.ItemAdded += OnItemAdded;
            locator.ItemDeleted += OnItemDeleted;

            foreach (var item in items)
            {
                await ProcessNewItem(item);
            }
        }

        private async void OnBeforeItemLocatorDeleted(object sender, ItemLocatorEventArgs e)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (!_itemsLocators.Remove(e.ItemType))
                    _logger.Error($"Item locator {e.ItemType} is not found in locators list but try to clean items list anyway.");

                var items = _items.Where(i => i.ItemType == e.ItemType).ToList();

                foreach (var item in items)
                {
                    RemoveItem(item);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async void OnItemAdded(object sender, ItemEventArgs e)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (!_itemsLocators.ContainsKey(e.ItemType))
                {
                    _logger.Error(
                        $"Can't process new item {e.ItemId} because items locator {e.ItemType} has not been processed yet");
                    return;
                }

                var locatorBridge = _itemsLocators[e.ItemType];
                var items = await locatorBridge.GetItems();
                var item = items.First(i => i.ItemId == e.ItemId);

                await ProcessNewItem(item);
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task ProcessNewItem(IItem item)
        {
            var existingItem = _items.FirstOrDefault(i => i.ItemId == item.ItemId);

            if (existingItem != null) return;

            if (item is IInitializable initializable)
            {
                _logger.Info($"Start {item.ItemId} item initialization...");

                await initializable.Initialize();

                _logger.Info($"Item {item.ItemId} has been initialized.");
            }

            _items.Add(item);

            _immutableItems = _items.ToImmutableList();

            ItemAdded?.Invoke(this, new ItemEventArgs(item.ItemId, item.ItemType));
        }

        private async void OnItemDeleted(object sender, ItemEventArgs e)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var item = _items.FirstOrDefault(i => i.ItemId == e.ItemId && i.ItemType == e.ItemType);

                if (item == null)
                {
                    _logger.Error($"Item with Id = {e.ItemId} is not found in items list.");
                    return;
                }

                RemoveItem(item);
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void RemoveItem(IItem item)
        {
            if (_items.Remove(item))
                _immutableItems = _items.ToImmutableList();
            else
                _logger.Error($"Item with Id = {item.ItemId} is not found in items list but emit event anyway.");

            ItemDeleted?.Invoke(this, new ItemEventArgs(item.ItemId, item.ItemType));
        }
    }
}