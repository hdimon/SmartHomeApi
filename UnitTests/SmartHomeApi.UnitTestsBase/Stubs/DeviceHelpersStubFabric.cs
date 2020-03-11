using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class DeviceHelpersStubFabric : IItemHelpersFabric
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