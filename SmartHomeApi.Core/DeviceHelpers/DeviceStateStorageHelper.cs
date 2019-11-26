using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.DeviceHelpers
{
    public class DeviceStateStorageHelper : IDeviceStateStorageHelper
    {
        private readonly IApiLogger _logger;

        public DeviceStateStorageHelper(IApiLogger logger)
        {
            _logger = logger;
        }

        public async Task SaveState(object state, string fileNamePattern)
        {
            var content = JsonConvert.SerializeObject(state, Formatting.Indented);

            var fileName = fileNamePattern + ".txt";

            try
            {
                await File.WriteAllTextAsync(fileName, content);
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