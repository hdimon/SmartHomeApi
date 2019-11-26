using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class DeviceHelpersStubFabric : IDeviceHelpersFabric
    {
        public IDeviceStateStorageHelper GetDeviceStateStorageHelper()
        {
            return new DeviceStateStorageStubHelper();
        }

        public IApiLogger GetApiLogger()
        {
            return new ApiStubLogger();
        }
    }
}