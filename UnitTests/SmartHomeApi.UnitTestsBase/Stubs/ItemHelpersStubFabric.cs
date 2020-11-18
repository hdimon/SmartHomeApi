using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ItemHelpersStubFabric : IItemHelpersFabric
    {
        public IItemStateStorageHelper GetItemStateStorageHelper()
        {
            return new ItemStateStorageStubHelper();
        }

        public IApiLogger GetApiLogger()
        {
            return new ApiStubLogger();
        }
    }
}