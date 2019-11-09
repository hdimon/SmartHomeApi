using System;
using System.Collections.Generic;
using System.Text;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class DeviceHelpersStubFabric : IDeviceHelpersFabric
    {
        public IDeviceStateStorageHelper GetDeviceStateStorageHelper()
        {
            return new DeviceStateStorageStubHelper();
        }
    }
}
