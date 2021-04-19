using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace StandardTestPluginWithDependency1
{
    public class StandardTestPluginWithDependency1Main : StandardItem
    {
        public StandardTestPluginWithDependency1Main(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager,
            helpersFabric, config)
        {
            var content = JsonConvert.SerializeObject("Test");

            helpersFabric.GetApiLogger().Info(content);
        }
    }
}