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
    public abstract class StateChangedSubscriberAbstract : IStateChangedSubscriber, IDisposable
    {
        protected readonly IApiManager Manager;
        protected readonly IItemHelpersFabric HelpersFabric;
        protected readonly IApiLogger Logger;
        protected CancellationTokenSource DisposingCancellationTokenSource = new CancellationTokenSource();

        protected StateChangedSubscriberAbstract(IApiManager manager, IItemHelpersFabric helpersFabric)
        {
            Manager = manager;
            HelpersFabric = helpersFabric;
            Logger = helpersFabric.GetApiLogger();
        }

        public async Task Notify(StateChangedEvent args)
        {
            await ProcessNotification(args);
        }

        protected abstract Task ProcessNotification(StateChangedEvent args);

        protected void CancelTasks(string itemType, ConcurrentQueue<CancellationTokenSource> cancellationTokenSources)
        {
            int emergencyCounter = 0;

            try
            {
                while (!cancellationTokenSources.IsEmpty)
                {
                    if (emergencyCounter > 1000)
                    {
                        Logger.Error("Emergency exit on cancelling tasks.");
                        return;
                    }

                    CancellationTokenSource source;
                    if (cancellationTokenSources.TryDequeue(out source))
                        source.Cancel();

                    emergencyCounter++;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private bool IsCancellationRequested(string itemType, CancellationToken cancellationToken,
            StateChangedEvent args)
        {
            var isCancellationRequested = cancellationToken.IsCancellationRequested;

            if (isCancellationRequested)
                Logger.Warning($"Commands execution has been aborted. Event: {args}.");

            return isCancellationRequested;
        }

        protected async Task ExecuteCommands(string itemType, IList<SetValueCommand> commands, StateChangedEvent args,
            int maxTries, int failoverActionIntervalSeconds, SemaphoreSlim _semaphoreSlim,
            ConcurrentQueue<CancellationTokenSource> ctSources)
        {
            try
            {
                CancelTasks(itemType, ctSources);

                await _semaphoreSlim.WaitAsync();

                CancelTasks(itemType, ctSources);

                var cancellationTokenSource = new CancellationTokenSource();
                ctSources.Enqueue(cancellationTokenSource);

                await ExecuteCommands(itemType, commands, args, maxTries, failoverActionIntervalSeconds,
                    cancellationTokenSource.Token);
            }
            finally
            {
                CancelTasks(itemType, ctSources);
                _semaphoreSlim.Release();
            }
        }

        protected async Task ExecuteCommands(string itemType, IList<SetValueCommand> commands,
            StateChangedEvent args, int maxTries, int failoverActionIntervalSeconds,
            CancellationToken cancellationToken)
        {
            if (commands == null)
                return;

            using var linkedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(DisposingCancellationTokenSource.Token,
                    cancellationToken);

            var cToken = linkedTokenSource.Token;

            if (IsCancellationRequested(itemType, cToken, args))
                return;

            var commandsToRun = commands;

            try
            {
                async Task ExecuteCommandsAction()
                {
                    if (IsCancellationRequested(itemType, cToken, args))
                        return;

                    var results = await Task
                                        .WhenAll(commandsToRun.Select(c =>
                                            Task.Run(() => Manager.SetValue(c.ItemId, c.Parameter, c.Value))))
                                        .ConfigureAwait(false);

                    if (results == null || results.All(r => r.Success))
                        return;

                    var failed = results.Where(r => !r.Success).Select(r => r.ItemId).ToList();

                    commandsToRun = commandsToRun.Where(t => failed.Contains(t.ItemId)).ToList();

                    Logger.Warning(GetFailedExecutionMessage(failoverActionIntervalSeconds, commandsToRun));

                    if (IsCancellationRequested(itemType, cToken, args))
                        return;

                    throw new Exception(GetFailedExecutionMessage(commandsToRun));
                }

                await AsyncHelpers.RetryOnFault(ExecuteCommandsAction, maxTries,
                                      () => Task.Delay(failoverActionIntervalSeconds * 1000, cToken))
                                  .ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                Logger.Warning($"Commands execution has been aborted. Event: {args}.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
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

        public virtual void Dispose()
        {
            try
            {
                Manager.UnregisterSubscriber(this);
                DisposingCancellationTokenSource.Cancel();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}