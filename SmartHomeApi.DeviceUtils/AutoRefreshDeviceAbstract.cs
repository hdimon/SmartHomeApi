using System;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.DeviceUtils
{
    public abstract class AutoRefreshDeviceAbstract : DeviceAbstract
    {
        private Task _worker;
        private int _requestFailureCount;

        protected int RequestFailureThreshold = 30;
        protected int RefreshIntervalMS = 1000;
        protected IItemState CurrentState;
        protected readonly ReaderWriterLock RwLock = new ReaderWriterLock();

        protected AutoRefreshDeviceAbstract(IDeviceHelpersFabric helpersFabric, IDeviceConfig config) : base(helpersFabric, config)
        {
            CurrentState = new ItemState(ItemId, ItemType);
        }

        protected abstract Task<IItemState> RequestData();

        protected override async Task InitializeDevice()
        {
            RunDataCollectorWorker();
        }

        private void RunDataCollectorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(AutoDataRefreshWorker).Unwrap().ContinueWith(
                    t =>
                    {
                        var test = t;
                    } /*Log.Error(t.Exception)*/,
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task AutoDataRefreshWorker()
        {
            while (true)
            {
                await Task.Delay(RefreshIntervalMS);

                var state = await RequestData();

                bool failed = false;
                foreach (var telemetryPair in CurrentState.States)
                {
                    if (!state.States.ContainsKey(telemetryPair.Key))
                    {
                        failed = true;

                        if (_requestFailureCount < RequestFailureThreshold)
                        {
                            //Take previous value
                            state.States.Add(telemetryPair.Key, telemetryPair.Value);
                        }
                    }
                }

                if (failed)
                {
                    _requestFailureCount++;

                    if (_requestFailureCount < RequestFailureThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Unstable;
                    }
                    else if (_requestFailureCount >= RequestFailureThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Lost;
                    }
                }
                else
                    state.ConnectionStatus = ConnectionStatus.Stable;

                SetStateSafely(state);
            }
        }

        protected void SetStateSafely(IItemState state)
        {
            try
            {
                RwLock.AcquireWriterLock(Timeout.Infinite);

                CurrentState = state;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            finally
            {
                RwLock.ReleaseWriterLock();
            }
        }

        public override IItemState GetState()
        {
            RwLock.AcquireReaderLock(Timeout.Infinite);

            IItemState state = CurrentState;

            RwLock.ReleaseReaderLock();

            return state;
        }
    }
}