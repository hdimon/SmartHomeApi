using System;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.ItemUtils
{
    public class ConnectionStatusWatchdog : IInitializable, IDisposable
    {
        private readonly int _nonStableTimeoutMs;
        private readonly int _lostTimeoutMs;
        private Action<string> _onStatusChange;
        private CancellationTokenSource _cts;
        private readonly object _lock = new object();
        private string _currentStatus = ConnectionStatus.Unknown;
        private bool _active = true;

        public bool IsInitialized { get; private set; }

        public ConnectionStatusWatchdog(int nonStableTimeoutMs, int lostTimeoutMs, Action<string> onStatusChange)
        {
            if (lostTimeoutMs <= nonStableTimeoutMs)
                throw new ArgumentException("lostTimeoutMs must be greater than nonStableTimeoutMs");

            _nonStableTimeoutMs = nonStableTimeoutMs;
            _lostTimeoutMs = lostTimeoutMs;
            _onStatusChange = onStatusChange;
        }

        public async Task Initialize()
        {
            IsInitialized = true;
            _cts = new CancellationTokenSource();

            await RunWorker();

            _onStatusChange?.Invoke(_currentStatus);
        }

        private async Task RunWorker()
        {
            _ = Task.Factory.StartNew(WorkerWrapper).Unwrap().ContinueWith(task =>
            {
                if (task.Exception != null) throw task.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task WorkerWrapper()
        {
            while (_active)
            {
                int delay = GetDelay();
                bool reset = false;

                try
                {
                    await Task.Delay(delay, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    reset = true;
                }

                if (!reset)
                {
                    if (_currentStatus == ConnectionStatus.Unknown || _currentStatus == ConnectionStatus.Stable)
                    {
                        _currentStatus = ConnectionStatus.Unstable;
                        _onStatusChange?.Invoke(_currentStatus);
                    }
                    else if (_currentStatus == ConnectionStatus.Unstable)
                    {
                        _currentStatus = ConnectionStatus.Lost;
                        _onStatusChange?.Invoke(_currentStatus);
                    }
                }
                else //Watchdog has been reset
                {
                    lock (_lock)
                    {
                        _cts.Cancel();
                        _cts.Dispose();

                        _cts = new CancellationTokenSource();
                    }

                    if (_currentStatus != ConnectionStatus.Stable)
                    {
                        _currentStatus = ConnectionStatus.Stable;
                        _onStatusChange?.Invoke(_currentStatus);
                    }
                }
            }
        }

        private int GetDelay()
        {
            if (_currentStatus == ConnectionStatus.Stable || _currentStatus == ConnectionStatus.Unknown)
                return _nonStableTimeoutMs;

            if (_currentStatus == ConnectionStatus.Unstable)
                return _lostTimeoutMs - _nonStableTimeoutMs;

            return _lostTimeoutMs;
        }

        public void Reset()
        {
            _cts?.Cancel();
        }

        public string GetStatus()
        {
            return _currentStatus;
        }

        public void Dispose()
        {
            _onStatusChange = null;
            _active = false;
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}