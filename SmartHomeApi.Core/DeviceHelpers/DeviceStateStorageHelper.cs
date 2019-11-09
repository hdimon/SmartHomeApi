using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.DeviceHelpers
{
    public class DeviceStateStorageHelper : IDeviceStateStorageHelper
    {
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

            }

            if (content == null)
                return obj;

            try
            {
                obj = JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception e)
            {

            }

            return obj;
        }
    }
}