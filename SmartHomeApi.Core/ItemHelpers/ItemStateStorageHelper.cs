using System;
using System.IO;
using System.Threading.Tasks;
using Common.Utils;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core.ItemHelpers
{
    public class ItemStateStorageHelper : IItemStateStorageHelper
    {
        private readonly IApiLogger _logger;
        private readonly string _storageDirectory;

        public ItemStateStorageHelper(IApiLogger logger, IOptionsMonitor<AppSettings> appSettingsMonitor)
        {
            _logger = logger;

            _storageDirectory = Path.Combine(appSettingsMonitor.CurrentValue.DataDirectoryPath, "PluginsStateStorage");
            Directory.CreateDirectory(_storageDirectory);
        }

        public async Task SaveState(object state, string fileNamePattern)
        {
            var options = new JsonSerializerSettings { Converters = { new NewtonsoftTimeSpanConverter() } };
            var content = JsonConvert.SerializeObject(state, Formatting.Indented, options);

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
                var options = new JsonSerializerSettings { Converters = { new NewtonsoftTimeSpanConverter() } };
                obj = JsonConvert.DeserializeObject<T>(content, options);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when deserializing state.");
            }

            return obj;
        }
    }
}