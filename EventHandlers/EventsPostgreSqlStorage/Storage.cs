using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace EventsPostgreSqlStorage
{
    class Storage : IStateChangedSubscriber, IConfigurable, IInitializable
    {
        public string ItemType => "EventsPostgreSqlStorage";
        private readonly IApiManager _manager;
        private readonly IItemConfig _config;

        public bool IsInitialized { get; private set; }

        public Storage(IApiManager manager, IItemConfig config)
        {
            _manager = manager;
            _config = config;
        }

        public async Task Notify(StateChangedEvent args)
        {
        }

        
        public async Task Initialize()
        {
            if (IsInitialized)
                return;

            _manager.RegisterSubscriber(this);

            IsInitialized = true;
        }
    }
}