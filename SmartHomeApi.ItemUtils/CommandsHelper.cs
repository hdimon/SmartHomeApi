using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.ItemUtils
{
    public class CommandsHelper
    {
        private readonly IApiManager _apiManager;
        private readonly IApiLogger _logger;
        private readonly CancellationTokenSource _ctSource;

        public CommandsHelper(IApiManager apiManager, IApiLogger logger, CancellationTokenSource ctSource)
        {
            _apiManager = apiManager;
            _logger = logger;
            _ctSource = ctSource;
        }

        protected void CancelTasks(ConcurrentQueue<CancellationTokenSource> cancellationTokenSources)
        {
            int emergencyCounter = 0;

            try
            {
                while (!cancellationTokenSources.IsEmpty)
                {
                    if (emergencyCounter > 1000)
                    {
                        _logger.Error("Emergency exit on cancelling tasks.");
                        return;
                    }

                    if (cancellationTokenSources.TryDequeue(out var source))
                        source.Cancel();

                    emergencyCounter++;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private bool IsCancellationRequested(CancellationToken cancellationToken, StateChangedEvent args)
        {
            var isCancellationRequested = cancellationToken.IsCancellationRequested;

            if (isCancellationRequested)
                _logger.Warning($"Commands execution has been aborted. Event: {args}.");

            return isCancellationRequested;
        }

        protected async Task ExecuteCommands(string itemType, IList<SetValueCommand> commands, StateChangedEvent args,
            int maxTries, int failoverActionIntervalSeconds, SemaphoreSlim _semaphoreSlim,
            ConcurrentQueue<CancellationTokenSource> ctSources)
        {
            try
            {
                CancelTasks(ctSources);

                await _semaphoreSlim.WaitAsync();

                CancelTasks(ctSources);

                var cancellationTokenSource = new CancellationTokenSource();
                ctSources.Enqueue(cancellationTokenSource);

                await ExecuteCommands(itemType, commands, args, maxTries, failoverActionIntervalSeconds,
                    cancellationTokenSource.Token);
            }
            finally
            {
                CancelTasks(ctSources);
                _semaphoreSlim.Release();
            }
        }

        protected async Task ExecuteCommands(string itemType, IList<SetValueCommand> commands, StateChangedEvent args,
            int maxTries, int failoverActionIntervalSeconds, CancellationToken cancellationToken)
        {
            if (commands == null)
                return;

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_ctSource.Token, cancellationToken);

            var cToken = linkedTokenSource.Token;

            if (IsCancellationRequested(cToken, args))
                return;

            var commandsToRun = commands;

            try
            {
                async Task ExecuteCommandsAction()
                {
                    if (IsCancellationRequested(cToken, args))
                        return;

                    var results = await Task
                                        .WhenAll(commandsToRun.Select(c =>
                                            Task.Run(() => _apiManager.SetValue(c.ItemId, c.Parameter, c.Value))))
                                        .ConfigureAwait(false);

                    if (results.All(r => r.Success))
                        return;

                    var failed = results.Where(r => !r.Success).Select(r => r.ItemId).ToList();

                    commandsToRun = commandsToRun.Where(t => failed.Contains(t.ItemId)).ToList();

                    _logger.Warning(GetFailedExecutionMessage(failoverActionIntervalSeconds, commandsToRun));

                    if (IsCancellationRequested(cToken, args))
                        return;

                    throw new Exception(GetFailedExecutionMessage(commandsToRun));
                }

                await AsyncHelpers
                      .RetryOnFault(ExecuteCommandsAction, maxTries,
                          () => Task.Delay(failoverActionIntervalSeconds * 1000, cToken)).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                _logger.Warning($"Commands execution has been aborted. Event: {args}.");
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }
        }

        private string GetFailedExecutionMessage(IList<SetValueCommand> failedCommands)
        {
            var message = new StringBuilder();

            message.AppendLine("Commands execution was failed. Failed commands: ");

            var commands = string.Join("," + Environment.NewLine, failedCommands);

            message.Append(commands);

            message.Append(".");

            return message.ToString();
        }

        private string GetFailedExecutionMessage(int failoverActionIntervalSeconds, IList<SetValueCommand> failedCommands)
        {
            var message = new StringBuilder();

            message.Append("Commands execution was not fully successful so try to execute it again in ");
            message.Append(failoverActionIntervalSeconds);
            message.AppendLine(" seconds. Failed commands: ");

            var commands = string.Join("," + Environment.NewLine, failedCommands);

            message.Append(commands);

            message.Append(".");

            return message.ToString();
        }

        protected SetValueCommand CreateCommand(string itemId, string parameter, object value)
        {
            var command = new SetValueCommand();
            command.ItemId = itemId;
            command.Parameter = parameter;
            command.Value = value;

            return command;
        }
    }
}