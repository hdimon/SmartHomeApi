using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.TestItem2Plugin
{
    class TestItem2Config : ItemConfigAbstract
    {
        public string TestString1 { get; set; }

        public TestItem2Config(string itemId, string itemType) : base(itemId, itemType)
        {
        }
    }
}