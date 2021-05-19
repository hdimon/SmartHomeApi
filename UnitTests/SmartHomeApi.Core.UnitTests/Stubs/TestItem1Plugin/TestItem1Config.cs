using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.TestItem1Plugin
{
    class TestItem1Config : ItemConfigAbstract
    {
        public string TestString { get; set; }

        public TestItem1Config(string itemId, string itemType) : base(itemId, itemType)
        {
        }
    }
}