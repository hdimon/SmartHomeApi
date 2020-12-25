using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ItemHelpersStubFabric : IItemHelpersFabric
    {
        public IItemStateStorageHelper GetItemStateStorageHelper()
        {
            return new ItemStateStorageStubHelper();
        }

        public IJsonSerializer GetJsonSerializer()
        {
            throw new System.NotImplementedException();
        }

        public IApiLogger GetApiLogger()
        {
            return new ApiStubLogger();
        }

        public IItemStateNew GetOrCreateItemState()
        {
            return null;
        }
    }
}