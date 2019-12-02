using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class DeviceHelpersStubFabric : IItemHelpersFabric
    {
        public IItemStateStorageHelper GetDeviceStateStorageHelper()
        {
            return new DeviceStateStorageStubHelper();
        }

        public IApiLogger GetApiLogger()
        {
            return new ApiStubLogger();
        }
    }
}