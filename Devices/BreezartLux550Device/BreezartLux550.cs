using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;

namespace BreezartLux550Device
{
    public class BreezartLux550 : DeviceAbstract
    {
        private TcpClient _client;
        private Task _worker;
        private IItemState CurrentState;
        private readonly ReaderWriterLock RwLock = new ReaderWriterLock();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private int _requestFailureCount;
        private int _requestFailureThreshold = 60;
        private Queue<string> _getCommandsQueue = new Queue<string>();
        private Queue<string> _setCommandsQueue = new Queue<string>();
        private string _requestStateCommand = "VSt07_FFFF";
        private string _requestSensorsCommand = "VSens_FFFF";

        public BreezartLux550(IDeviceHelpersFabric helpersFabric, IDeviceConfig config) : base(helpersFabric, config)
        {
            CurrentState = new ItemState(ItemId, ItemType);
        }

        protected override async Task InitializeDevice()
        {
            var config = (BreezartLux550Config)Config;
            _client = new TcpClient(config.IpAddress, 1560);

            _getCommandsQueue.Enqueue(_requestStateCommand);
            _getCommandsQueue.Enqueue(_requestSensorsCommand);

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
                await Task.Delay(500);

                string command;

                if (_setCommandsQueue.Any())
                {
                    command = _setCommandsQueue.Dequeue();
                }
                else
                {
                    command = _getCommandsQueue.Dequeue();
                    _getCommandsQueue.Enqueue(command);
                }

                var state = await RequestData(command);

                bool failed = state == null;

                if (failed)
                {
                    _requestFailureCount++;

                    if (_requestFailureCount < _requestFailureThreshold)
                    {
                        CurrentState.ConnectionStatus = ConnectionStatus.Unstable;
                    }
                    else if (_requestFailureCount >= _requestFailureThreshold)
                    {
                        CurrentState.ConnectionStatus = ConnectionStatus.Lost;
                    }
                }
                else
                {
                    state.ConnectionStatus = ConnectionStatus.Stable;

                    foreach (var telemetryPair in CurrentState.States)
                    {
                        if (!state.States.ContainsKey(telemetryPair.Key))
                        {
                            state.States.Add(telemetryPair.Key, telemetryPair.Value);
                        }
                    }

                    state.States = new Dictionary<string, object>(state.States.OrderBy(s => s.Key));

                    SetStateSafely(state);
                }
            }
        }

        private async Task<IItemState> RequestData(string command)
        {
            var responseData = await SendCommand(command);

            if (string.IsNullOrWhiteSpace(responseData))
                return null;

            var state = new ItemState(ItemId, ItemType);

            var result = responseData.Split("_");

            if (command == _requestStateCommand)
            {
                var modeBinarystring = ConvertParameterValueToBitsString(result[2]);
                int unitState = Convert.ToInt32(modeBinarystring.Substring(14, 2), 2);
                state.States.Add("UnitState", GetUnitStateString(unitState));

                var temperaturesBinarystring = ConvertParameterValueToBitsString(result[3]);
                int setTemp = Convert.ToInt32(temperaturesBinarystring.Substring(0, 8), 2);
                state.States.Add("SetTemperature", setTemp);
                int currentTemp = Convert.ToInt32(temperaturesBinarystring.Substring(8, 8), 2);
                state.States.Add("CurrentTemperature", currentTemp);

                var speedBinarystring = ConvertParameterValueToBitsString(result[5]);
                int setSpeed = Convert.ToInt32(speedBinarystring.Substring(8, 4), 2);
                state.States.Add("SetSpeed", setSpeed);
                int currentSpeed = Convert.ToInt32(speedBinarystring.Substring(12, 4), 2);
                state.States.Add("CurrentSpeed", currentSpeed);
                int factSpeedPercent = Convert.ToInt32(speedBinarystring.Substring(0, 8), 2);
                //state.States.Add("FactSpeedPercent", factSpeedPercent);

                var miscBinarystring = ConvertParameterValueToBitsString(result[6]);
                int filterDustPercent = Convert.ToInt32(miscBinarystring.Substring(0, 8), 2);
                state.States.Add("FilterDustPercent", filterDustPercent);
            }
            else if (command == _requestSensorsCommand)
            {
                var outTempBinarystring = ConvertParameterValueToBitsString(result[1]);
                int outTemp = Convert.ToInt32(outTempBinarystring, 2); //Divide on 10

                var powerBinarystring = ConvertParameterValueToBitsString(result[8]);
                int power = Convert.ToInt32(powerBinarystring, 2);
                state.States.Add("Power", power);
            }

            return state;
        }

        private async Task<string> SendCommand(string command, int maxTries = 1)
        {
            string responseString = null;

            try
            {
                responseString =
                    await AsyncHelpers.RetryOnFault(() => SendCommandWithLock(command), maxTries,
                        () => Task.Delay(500));
            }
            catch (Exception e)
            {
                // ignored
            }

            return responseString;
        }

        private async Task<string> SendCommandWithLock(string command)
        {
            string responseString;

            try
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(command);

                await _semaphoreSlim.WaitAsync();

                NetworkStream stream = _client.GetStream();

                // Send the message to the connected TcpServer. 
                await stream.WriteAsync(data, 0, data.Length);

                data = new byte[256];

                var bytes = await stream.ReadAsync(data, 0, data.Length);
                responseString = System.Text.Encoding.UTF8.GetString(data, 0, bytes);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return responseString;
        }

        private string ConvertParameterValueToBitsString(string value, int padToBytes = 2)
        {
            string binarystring = string.Join(string.Empty,
                value.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));

            binarystring = binarystring.PadLeft(padToBytes * 8, '0');

            return binarystring;
        }

        private string GetUnitStateString(int unitState)
        {
            switch (unitState)
            {
                case 0:
                    return "Off";
                case 1:
                    return "On";
                case 2:
                    return "TurningOff";
                case 3:
                    return "TurningOn";
                default:
                    return "Unknown";
            }
        }

        public override Task<ISetValueResult> SetValue(string parameter, string value)
        {
            throw new NotImplementedException();
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
                /*Console.WriteLine(e);
                throw;*/
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