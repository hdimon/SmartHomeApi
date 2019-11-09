using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class DeviceStateStorageStubHelper : IDeviceStateStorageHelper
    {
        public async Task SaveState(object state, string fileNamePattern)
        {
            
        }

        public T RestoreState<T>(string fileNamePattern)
        {
            return default;
        }
    }
}
