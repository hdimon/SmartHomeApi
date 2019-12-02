using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using SmartHomeApi.Core.Interfaces;

namespace EventsPostgreSqlStorage
{
    class Storage : IStateChangedSubscriber, IConfigurable, IInitializable
    {
        public string ItemType => "EventsPostgreSqlStorage";
        private readonly IApiManager _manager;
        private readonly IItemHelpersFabric _helpersFabric;
        private readonly StorageConfig _config;
        private readonly IApiLogger _logger;

        public bool IsInitialized { get; private set; }

        public Storage(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config)
        {
            _manager = manager;
            _helpersFabric = helpersFabric;
            _logger = _helpersFabric.GetApiLogger();
            _config = (StorageConfig)config;
        }

        public async Task Initialize()
        {
            if (IsInitialized)
                return;

            _manager.RegisterSubscriber(this);

            IsInitialized = true;

            var initEvent = new EventItem
            {
                DeviceId = "System",
                DeviceType = "System",
                Parameter = "ApiState",
                NewValue = "Started",
                EventType = StateChangedEventType.ValueAdded.ToString(),
                EventDate = DateTimeOffset.Now
            };

            await SaveEvent(initEvent, true);
        }

        private async Task SaveEvent(EventItem eventItem, bool rethrowExceptionOnFault = false)
        {
            try
            {
                using (NpgsqlConnection dbConnection = new NpgsqlConnection(_config.ConnectionString))
                {
                    await dbConnection.OpenAsync();
                    await dbConnection.ExecuteAsync(
                        "INSERT INTO \"Events\" (\"EventDate\",\"EventType\",\"DeviceType\",\"DeviceId\",\"Parameter\",\"OldValue\",\"NewValue\") " +
                        "VALUES(@EventDate,@EventType,@DeviceType,@DeviceId,@Parameter,@OldValue,@NewValue)",
                        eventItem);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);

                if (rethrowExceptionOnFault)
                    throw;
            }
        }

        public async Task Notify(StateChangedEvent args)
        {
            var eventItem = new EventItem(args);

            await SaveEvent(eventItem);
        }
    }
}