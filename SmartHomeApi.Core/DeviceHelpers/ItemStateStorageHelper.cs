using System;
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

        public ItemStateStorageHelper(IApiLogger logger)
        {
            _logger = logger;
        }

        public async Task SaveState(object state, string fileNamePattern)
        {
            var content = JsonConvert.SerializeObject(state, Formatting.Indented);

            var fileName = fileNamePattern + ".txt";

            try
            {
                await AsyncHelpers.RetryOnFault(() => File.WriteAllTextAsync(fileName, content), 5,
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

            var fileName = fileNamePattern + ".txt";

            string content = null;

            try
            {
                content = File.ReadAllText(fileName);
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