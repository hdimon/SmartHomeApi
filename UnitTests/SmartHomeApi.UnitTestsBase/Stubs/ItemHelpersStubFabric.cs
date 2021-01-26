using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.ItemHelpers;
using SmartHomeApi.Core.Services;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ItemHelpersStubFabric : IItemHelpersFabric
    {
        private string _itemId;
        private string _itemType;
        private ISmartHomeApiFabric _fabric;
        private IItemStatesProcessor _statesProcessor;

        public ItemHelpersStubFabric(string itemId, string itemType, ISmartHomeApiFabric fabric)
        {
            _itemId = itemId;
            _itemType = itemType;
            _fabric = fabric;
            _statesProcessor = new ItemStatesProcessor(fabric);
        }

        public IItemStateStorageHelper GetItemStateStorageHelper()
        {
            return new ItemStateStorageStubHelper();
        }

        public IJsonSerializer GetJsonSerializer()
        {
            return new NewtonsoftJsonSerializer();
        }

        public IApiLogger GetApiLogger()
        {
            return new ApiStubLogger();
        }

        public IDateTimeOffsetProvider GetDateTimeOffsetProvider()
        {
            return new FakeDateTimeOffsetProvider();
        }

        public IItemStateNew GetOrCreateItemState()
        {
            return _statesProcessor.GetOrCreateItemState(_itemId, _itemType);
        }
    }
}