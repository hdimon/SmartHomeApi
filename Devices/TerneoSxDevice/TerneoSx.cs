using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.DeviceUtils;

namespace TerneoSxDevice
{
    public class TerneoSx : DeviceAbstract
    {
        private IDeviceState _state;
        private Task _worker;
        private readonly HttpClient _client = new HttpClient();
        private readonly ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private readonly SemaphoreSlim _semaphoreSlimHttpPost = new SemaphoreSlim(1, 1);
        private int _requestFailureCount;
        private readonly int _requestFailureThreshold = 30;
        private readonly AverageValuesHelper _currentTempAverageValuesHelper = new AverageValuesHelper(10);

        private readonly string CurrentTemperatureParameter = "CurrentTemperature";
        private readonly string SetTemperatureParameter = "SetTemperature";
        private readonly string HeatingParameter = "Heating";

        private readonly List<string> _settableParametersList;

        public TerneoSx(IDeviceHelpersFabric helpersFabric, IDeviceConfig config) : base(helpersFabric, config)
        {
            _settableParametersList = new List<string> { SetTemperatureParameter };

            _state = new DeviceState(DeviceId, DeviceType);
            RunTelemetryCollectorWorker();
        }

        public override IDeviceState GetState()
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            IDeviceState state = _state;

            _readerWriterLock.ReleaseReaderLock();

            return state;
        }

        public override async Task<ISetValueResult> SetValue(string parameter, string value)
        {
            var result = new SetValueResult();

            if (!_settableParametersList.Contains(parameter))
            {
                result.Success = false;
                return result;
            }

            var config = (TerneoSxConfig)Config;

            var content = new StringContent($"{{\"sn\":\"{config.SerialNumber}\", \"par\":[[5,1,\"{value}\"]]}}", Encoding.UTF8,
                "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            string responseString = await PostContent(content, 15);

            if (string.IsNullOrWhiteSpace(responseString))
            {
                result.Success = false;
                return result;
            }

            return result;
        }

        private async Task<string> PostContent(StringContent content, int maxTries = 1)
        {
            string responseString = null;

            try
            {
                responseString =
                    await AsyncHelpers.RetryOnFault(() => PostContentWithLock(content), maxTries,
                        () => Task.Delay(2000));
            }
            catch (Exception e)
            {
                // ignored
            }

            return responseString;
        }

        private async Task<string> PostContentWithLock(StringContent content)
        {
            var config = (TerneoSxConfig)Config;
            string responseString;

            try
            {
                await _semaphoreSlimHttpPost.WaitAsync();

                var response = await _client.PostAsync($"http://{config.IpAddress}/api.cgi", content);
                responseString = await response.Content.ReadAsStringAsync();
            }
            finally
            {
                _semaphoreSlimHttpPost.Release();
            }

            return responseString;
        }

        private void RunTelemetryCollectorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(GetTelemetry).Unwrap().ContinueWith(
                    t =>
                    {
                        var test = t;
                    } /*Log.Error(t.Exception)*/,
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task GetTelemetry()
        {
            while (true)
            {
                await Task.Delay(1000);

                var state = await RequestTelemetry();

                bool failed = false;
                foreach (var telemetryPair in _state.Telemetry)
                {
                    if (!state.Telemetry.ContainsKey(telemetryPair.Key))
                    {
                        failed = true;

                        if (_requestFailureCount < _requestFailureThreshold)
                        {
                            //Take previous value
                            state.Telemetry.Add(telemetryPair.Key, telemetryPair.Value);
                        }
                    }
                }

                if (failed)
                {
                    _requestFailureCount++;

                    if (_requestFailureCount < _requestFailureThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Unstable;
                    }
                    else if (_requestFailureCount >= _requestFailureThreshold)
                    {
                        state.ConnectionStatus = ConnectionStatus.Lost;
                    }
                }
                else
                    state.ConnectionStatus = ConnectionStatus.Stable;

                SetStateSafely(state);
            }
        }

        private async Task<IDeviceState> RequestTelemetry()
        {
            var state = new DeviceState(DeviceId, DeviceType);

            var content = new StringContent("{\"cmd\":4}", Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            string responseString = await PostContent(content);

            if (string.IsNullOrWhiteSpace(responseString))
                return state;

            Dictionary<string, string> telemetry = null;

            try
            {
                telemetry = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }
            catch (Exception e)
            {
                
            }

            if (telemetry == null)
                return state;

            if (telemetry.ContainsKey("success") && telemetry["success"] == "false")
                return state;

            if (telemetry.ContainsKey("t.1"))
            {
                if (double.TryParse(telemetry["t.1"], out var currentTemp))
                    currentTemp = _currentTempAverageValuesHelper.GetAverageValue(Math.Round(currentTemp / 16, 1));

                state.Telemetry.Add(CurrentTemperatureParameter, Math.Round(currentTemp, 1));
            }
            if (telemetry.ContainsKey("t.5"))
            {
                double.TryParse(telemetry["t.5"], out var setTemp);
                state.Telemetry.Add(SetTemperatureParameter, Math.Round(setTemp / 16, 1));
            }
            if (telemetry.ContainsKey("f.0"))
            {
                var heating = telemetry["f.0"] == "1";

                state.Telemetry.Add(HeatingParameter, heating);
            }

            return state;
        }

        private void SetStateSafely(IDeviceState state)
        {
            try
            {
                _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

                _state = state;
            }
            catch (Exception e)
            {
                /*Console.WriteLine(e);
                throw;*/
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }
    }
}