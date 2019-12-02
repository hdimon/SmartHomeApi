using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.DeviceUtils
{
    public abstract class AutoRefreshDeviceAbstract : DeviceAbstract
    {
        private Task _worker;
        private int _requestFailureCount;

        protected int RequestFailureMinThreshold = 5;
        protected int RequestFailureMaxThreshold = 30;
        protected int RefreshIntervalMS = 1000;
        protected IItemState CurrentState;
        protected readonly ReaderWriterLock RwLock = new ReaderWriterLock();

        protected AutoRefreshDeviceAbstract(IItemHelpersFabric helpersFabric, IItemConfig config) : base(helpersFabric, config)
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
                        Logger.Error(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task AutoDataRefreshWorker()
        {
            while (true)
            {
                await Task.Delay(RefreshIntervalMS);

                bool failed = false;

                var state = await RequestData();
                ExtendItemStates(state);

                if (!CurrentState.States.Any() && !state.States.Any())
                    failed = true;

                foreach (var telemetryPair in CurrentState.States)
                {
                    if (!state.States.ContainsKey(telemetryPair.Key))
                    {
                        failed = true;

                        if (_requestFailureCount < RequestFailureMaxThreshold)
                        {
                            //Take previous value
                            state.States.Add(telemetryPair.Key, telemetryPair.Value);
                        }
                    }
                }

                if (failed)
                {
                    _requestFailureCount++;

                    if (_requestFailureCount < RequestFailureMinThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Stable;
                    }
                    else if (_requestFailureCount < RequestFailureMaxThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Unstable;
                    }
                    else if (_requestFailureCount >= RequestFailureMaxThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Lost;
                    }
                }
                else
                {
                    _requestFailureCount = 0;
                    state.ConnectionStatus = ConnectionStatus.Stable;
                }

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

        protected virtual void ExtendItemStates(IItemState state)
        {
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