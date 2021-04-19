using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace StandardTestPlugin1
{
    public class StandardTestPlugin1Main : StandardItem
    {
        public StandardTestPlugin1Main(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager,
            helpersFabric, config)
        {
        }
    }
}