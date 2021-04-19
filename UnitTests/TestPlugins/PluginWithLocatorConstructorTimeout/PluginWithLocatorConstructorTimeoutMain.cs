using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace PluginWithLocatorConstructorTimeout
{
    public class PluginWithLocatorConstructorTimeoutMain : StandardItem
    {
        public PluginWithLocatorConstructorTimeoutMain(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager,
            helpersFabric, config)
        {
        }
    }
}