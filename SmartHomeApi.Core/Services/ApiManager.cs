using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Services
{
    public class ApiManager : IApiManager
    {
        private bool _disposed;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IItemStatesProcessor _statesProcessor;
        private readonly IApiLogger _logger;
        private readonly IApiItemsLocator _apiItemsLocator;
        private readonly INotificationsProcessor _notificationsProcessor;
        private readonly IUncachedStatesProcessor _uncachedStatesProcessor;
        private readonly IDynamicToObjectMapper _dynamicToObjectMapper;
        private readonly IObjectToDynamicConverter _objectToDynamicConverter;
        private readonly CancellationTokenSource _disposingCancellationTokenSource = new CancellationTokenSource();

        public string ItemType => null;
        public string ItemId => null;
        public bool IsInitialized { get; private set; }

        public ApiManager(ISmartHomeApiFabric fabric, IItemStatesProcessor statesProcessor)
        {
            _fabric = fabric;
            _statesProcessor = statesProcessor;
            _logger = _fabric.GetApiLogger();
            _apiItemsLocator = _fabric.GetApiItemsLocator();
            _notificationsProcessor = _fabric.GetNotificationsProcessor();
            _uncachedStatesProcessor = _fabric.GetUncachedStatesProcessor();
            _dynamicToObjectMapper = _fabric.GetDynamicToObjectMapper();
            _objectToDynamicConverter = _fabric.GetObjectToDynamicConverter();
        }

        public async Task Initialize()
        {
            _logger.Info("Starting ApiManager initialization...");

            //First collect all initial plugins and then item configs
            await _fabric.GetItemsPluginsLocator().Initialize();
            await _fabric.GetItemsConfigsLocator().Initialize();
            await _apiItemsLocator.Initialize();

            IsInitialized = true;

            _logger.Info("ApiManager initialized.");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _logger.Info("Disposing ApiManager...");

            _disposingCancellationTokenSource.Cancel();

            var notificationsProcessor = _fabric.GetNotificationsProcessor();
            var pluginsLocator = _fabric.GetItemsPluginsLocator();
            var configLocator = _fabric.GetItemsConfigsLocator();

            await _apiItemsLocator.DisposeAsync();
            await notificationsProcessor.DisposeAsync();
            await pluginsLocator.DisposeAsync();
            await configLocator.DisposeAsync();

            _logger.Info("ApiManager has been disposed.");

            _disposed = true;
        }

        public async Task<ISetValueResult> SetValue(string itemId, string parameter, object value)
        {
            var items = await _apiItemsLocator.GetItems();

            var item = items.FirstOrDefault(i => i is IStateSettable it && it.ItemId == itemId);

            if (item == null)
                return new SetValueResult(itemId, null, false);

            var not = (IStateSettable)item;

            var currentParameterState = await GetState(not.ItemId, parameter);

            var ev = new StateChangedEvent(StateChangedEventType.ValueSet, not.ItemType, not.ItemId, parameter,
                currentParameterState, value);

            _notificationsProcessor.NotifySubscribers(ev);

            ISetValueResult result = null;

            try
            {
                result = await not.SetValue(parameter, value);

                result = new SetValueResult(not.ItemId, not.ItemType, result.Success);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return result ?? new SetValueResult(not.ItemId, not.ItemType, false);
        }

        public Task<IStatesContainer> GetState()
        {
            var state = _statesProcessor.GetStatesContainer();

            state = _uncachedStatesProcessor.FilterOutUncachedStates(state);

            return Task.FromResult(state);
        }

        public Task<IItemStateModel> GetState(string itemId)
        {
            var state = _statesProcessor.GetItemState(itemId);

            return Task.FromResult(state);
        }

        public Task<object> GetState(string itemId, string parameter)
        {
            var state = _statesProcessor.GetItemState(itemId, parameter);

            return Task.FromResult(state);
        }

        public async Task<IList<IItem>> GetItems()
        {
            var items = await _apiItemsLocator.GetItems();

            return items.ToList();
        }

        public async Task<object> Execute(string itemId, string command, object data, Type resultType = null)
        {
            var items = await _apiItemsLocator.GetItems();

            var item = items.FirstOrDefault(i => i is IExecutable it && it.ItemId == itemId);

            if (item == null)
                throw new ArgumentException($"Item {itemId} not found.");

            var eItem = (IExecutable)item;

            var methods = item.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                              .Where(m => m.GetCustomAttributes(typeof(ExecutableAttribute), true).Length > 0).ToArray();

            var methodsByCommandName =
                methods.Where(m => string.Equals(m.Name, command, StringComparison.CurrentCultureIgnoreCase)).ToList();

            if (methodsByCommandName.Count == 0)
                throw new ArgumentException($"Command {command} not found.");

            if (methodsByCommandName.Count > 1)
                throw new ValidationException($"More than one Command {command} found.");

            var method = methodsByCommandName.First();

            var inputParameters = method.GetParameters();

            if (inputParameters.Length > 1)
                throw new ValidationException(
                    $"Command {command} has {inputParameters.Length} input parameters. Only one input parameter is supported.");

            var parameters = CollectParameterForExecuteMethod(inputParameters, data);

            try
            {
                var task = (Task)method.Invoke(eItem, parameters);

                if (task == null)
                    throw new ValidationException($"Command {command} method should return Task<T>.");

                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                var result = resultProperty!.GetValue(task);

                if (!method.ReturnType.IsGenericType) return new ExecuteCommandResultVoid(); //Method returns Task, not Task<T>
                if (result is ExecuteCommandResultAbstract) return result;

                //Make copy of result in order not to block original type from plugin
                dynamic dynamicResult = _objectToDynamicConverter.Convert(result);

                if (resultType == null) return dynamicResult;

                var typedResult = _dynamicToObjectMapper.Map(dynamicResult, resultType);
                return typedResult;
            }
            catch (Exception e)
            {
                _logger.Error(e);

                var message = e is TargetInvocationException && e.InnerException != null ? e.InnerException.Message : e.Message;

                throw new ApplicationException(message);
            }
        }

        public void RegisterSubscriber(IStateChangedSubscriber subscriber)
        {
            _notificationsProcessor.RegisterSubscriber(subscriber);
        }

        public void UnregisterSubscriber(IStateChangedSubscriber subscriber)
        {
            _notificationsProcessor.UnregisterSubscriber(subscriber);
        }

        public void NotifySubscribers(StateChangedEvent args)
        {
            _notificationsProcessor.NotifySubscribers(args);
        }

        private object[] CollectParameterForExecuteMethod(ParameterInfo[] inputParameters, object data)
        {
            if (inputParameters.Length != 1) return null;

            var parameters = new object[1];

            var dynamicData = data is ExpandoObject expandoObject ? expandoObject : _objectToDynamicConverter.Convert(data);

            var param = _dynamicToObjectMapper.Map(dynamicData, inputParameters[0].ParameterType);

            parameters[0] = param;

            return parameters;
        }
    }
}