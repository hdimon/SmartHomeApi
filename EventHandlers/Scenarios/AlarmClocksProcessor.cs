using System;
using System.Globalization;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    class AlarmClocksProcessor : StateChangedSubscriberAbstract
    {
        public AlarmClocksProcessor(IApiManager manager, IItemHelpersFabric helpersFabric) : base(manager,
            helpersFabric)
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {
            if (args == null)
                return;

            if (args.EventType == StateChangedEventType.ValueSet)
                return;

            switch (args.ItemId)
            {
                case "Virtual_States":
                    await ProcessVirtualStateEvents(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessVirtualStateEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "DimaSonyExperiaAlarmClockMs":
                    await ProcessVirtualStateDimaSonyExperiaAlarmClockMsEvents(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessVirtualStateDimaSonyExperiaAlarmClockMsEvents(StateChangedEvent args)
        {
            if (args.NewValue == "0")
            {
                await Manager.SetValue("Virtual_MainAlarmClock", "Enabled", "false").ConfigureAwait(false);
            }
            else
            {
                DateTime dateTime = new DateTime(1970, 1, 1);

                dateTime = dateTime.AddMilliseconds(Convert.ToDouble(args.NewValue, CultureInfo.InvariantCulture)).ToLocalTime();

                var aalarmTimeStr = dateTime.ToLongTimeString();

                await Manager
                      .SetValue("Virtual_MainAlarmClock", "Time", aalarmTimeStr).ConfigureAwait(false);
                await Manager.SetValue("Virtual_MainAlarmClock", "Enabled", "true").ConfigureAwait(false);
            }
        }
    }
}