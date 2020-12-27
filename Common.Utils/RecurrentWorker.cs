using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utils
{
    public class RecurrentWorker
    {
        private Task _worker;
        private volatile bool _isFirstRun = true;
        private readonly CancellationToken _cToken;
        private readonly Action<Exception> _onException;
        private readonly Func<CancellationToken, Task> _preAction;
        private readonly Func<CancellationToken, Task> _action;

        public RecurrentWorker(CancellationToken cToken, Func<CancellationToken, Task> preAction,
            Func<CancellationToken, Task> action, Action<Exception> onException)
        {
            _cToken = cToken;
            _preAction = preAction;
            _action = action;
            _onException = onException;
        }

        public void Run()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(WorkerWrapper).Unwrap().ContinueWith(
                    t => { _onException?.Invoke(t.Exception); },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task WorkerWrapper()
        {
            while (!_cToken.IsCancellationRequested)
            {
                if (!_isFirstRun)
                    await _preAction(_cToken);

                if (_isFirstRun)
                    _isFirstRun = false;

                try
                {
                    await _action(_cToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _onException?.Invoke(e);
                }
            }
        }
    }
}