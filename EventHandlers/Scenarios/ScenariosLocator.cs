using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class ScenariosLocator : IItemsLocator
    {
        public string ItemType => "Scenario";
        public bool ImmediateInitialization => true;

        private readonly ISmartHomeApiFabric _fabric;

        private readonly List<IItem> _scenarios = new List<IItem>();

        public ScenariosLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            var manager = _fabric.GetApiManager();

            _scenarios.Add(new HeatingSystem(manager));
            _scenarios.Add(new VentilationSystem(manager));
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            return _scenarios;
        }
    }
}