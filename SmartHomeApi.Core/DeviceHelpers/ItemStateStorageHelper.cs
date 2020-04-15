﻿using System;
using System.IO;
using System.Threading.Tasks;
using Common.Utils;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.DeviceHelpers
{
    public class ItemStateStorageHelper : IItemStateStorageHelper
    {
        private readonly IApiLogger _logger;
        private readonly string _storageDirectory;

        public ItemStateStorageHelper(IApiLogger logger)
        {
            _logger = logger;

            _storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "PluginsStateStorage");
            Directory.CreateDirectory(_storageDirectory);
        }

        public async Task SaveState(object state, string fileNamePattern)
        {
            var content = JsonConvert.SerializeObject(state, Formatting.Indented);

            var fileName = fileNamePattern + ".json";
            var filePath = Path.Combine(_storageDirectory, fileName);

            try
            {
                await AsyncHelpers.RetryOnFault(() => File.WriteAllTextAsync(filePath, content), 5,
                    () => Task.Delay(2000));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when saving state to file.");
            }
        }

        public T RestoreState<T>(string fileNamePattern)
        {
            T obj = default;

            var fileName = fileNamePattern + ".json";
            var filePath = Path.Combine(_storageDirectory, fileName);

            string content = null;

            try
            {
                content = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when restoring state from file.");
            }

            if (content == null)
                return obj;

            try
            {
                obj = JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when deserializing state.");
            }

            return obj;
        }
    }
}