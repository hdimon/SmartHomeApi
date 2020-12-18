using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.ItemUtils
{
    public abstract class AutoRefreshItemAbstract : ItemAbstract
    {
        private Task _worker;
        private int _requestFailureCount;
        private volatile bool _isFirstRun = true;

        protected int RequestFailureMinThreshold = 5;
        protected int RequestFailureMaxThreshold = 30;
        protected int RefreshIntervalMS = 1000;
        protected IItemState CurrentState;
        protected readonly ReaderWriterLock RwLock = new ReaderWriterLock();

        protected AutoRefreshItemAbstract(IItemHelpersFabric helpersFabric, IItemConfig config) : base(helpersFabric, config)
        {
            CurrentState = new ItemState(ItemId, ItemType);
        }

        protected abstract Task<IItemState> RequestData();

        protected override async Task InitializeItem()
        {
            RunDataCollectorWorker();
        }

        private void RunDataCollectorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(DataCollectorWorkerWrapper).Unwrap().ContinueWith(
                    t =>
                    {
                        Logger.Error(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task DataCollectorWorkerWrapper()
        {
            while (!DisposingCancellationTokenSource.IsCancellationRequested)
            {
                if (!_isFirstRun)
                    await Task.Delay(RefreshIntervalMS, DisposingCancellationTokenSource.Token);

                if (_isFirstRun)
                    _isFirstRun = false;

                try
                {
                    await AutoDataRefreshWorker();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private async Task AutoDataRefreshWorker()
        {
            bool failed = false;

            var state = await RequestData();
            state.States["ConnectionStatus"] = ConnectionStatus.Stable;
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
                    state.States["ConnectionStatus"] = ConnectionStatus.Stable;
                }
                else if (_requestFailureCount < RequestFailureMaxThreshold)
                {
                    state.States["ConnectionStatus"] = ConnectionStatus.Unstable;
                }
                else if (_requestFailureCount >= RequestFailureMaxThreshold)
                {
                    state.States["ConnectionStatus"] = ConnectionStatus.Lost;
                }
            }
            else
            {
                _requestFailureCount = 0;
                state.States["ConnectionStatus"] = ConnectionStatus.Stable;
            }

            SetStateSafely(state);
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