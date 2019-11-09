using System.Collections.Generic;
using System.Threading.Tasks;
using Scenarios;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class EventHandlerLocator : IEventHandlerLocator
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly List<IEventHandler> _eventHandlers = new List<IEventHandler>();

        public EventHandlerLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public async Task Initialize()
        {
            var manager = _fabric.GetDeviceManager();

            _eventHandlers.Add(new HeatingSystem(manager));
        }
    }
}