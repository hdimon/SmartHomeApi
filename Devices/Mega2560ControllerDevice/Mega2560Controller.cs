using SmartHomeApi.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using Newtonsoft.Json;
using SmartHomeApi.DeviceUtils;

namespace Mega2560ControllerDevice
{
    public class Mega2560Controller : AutoRefreshDeviceAbstract, IStateTransformable
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly SemaphoreSlim _semaphoreSlimHttpPost = new SemaphoreSlim(1, 1);
        private readonly AverageValuesHelper _currentTempAverageValues = new AverageValuesHelper(10);
        private readonly AverageValuesHelper _currentCO2AverageValues = new AverageValuesHelper(40);
        private readonly AverageValuesHelper _currentHumidityAverageValues = new AverageValuesHelper(10);
        private readonly AverageValuesHelper _currentPressureHPaAverageValues = new AverageValuesHelper(1000);
        private int? _previousCO2;
        private int? _previousHumidityPercent;
        private double? _previousTemperatureC;
        private readonly List<string> _availablePinCommands = new List<string> { "high", "low", "pimp", "nimp" };

        public Mega2560Controller(IItemHelpersFabric helpersFabric, IItemConfig config) : base(helpersFabric,
            config)
        {
            RefreshIntervalMS = 500;
        }

        protected override Task InitializeDevice()
        {
            _client.Timeout = new TimeSpan(0, 0, 10);

            return base.InitializeDevice();
        }

        protected override async Task<IItemState> RequestData()
        {
            var content = new StringContent("", Encoding.UTF8, "application/text");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/text");

            string responseString = await PostContent(content);

            var state = ParseState(responseString);

            return state;
        }

        private ItemState ParseState(string responseString)
        {
            var config = (Mega2560ControllerConfig)Config;
            var state = new ItemState(ItemId, ItemType);

            if (string.IsNullOrWhiteSpace(responseString))
                return state;

            Dictionary<string, string> telemetry = null;

            try
            {
                telemetry = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            if (telemetry == null)
                return state;

            if (telemetry.ContainsKey("Mac"))
            {
                state.States.Add("Mac", telemetry["Mac"]);
            }

            if (telemetry.ContainsKey("CO2ppm") && config.HasCO2Sensor)
            {
                if (double.TryParse(telemetry["CO2ppm"], out var currentCO2))
                    currentCO2 = _currentCO2AverageValues.GetAverageValue(currentCO2);

                var roundedCurrentCO2 = (int)Math.Round(currentCO2, 0);

                if (!_previousCO2.HasValue)
                    _previousCO2 = roundedCurrentCO2;
                else
                {
                    if (Math.Abs(roundedCurrentCO2 - _previousCO2.Value) > 2)
                        _previousCO2 = roundedCurrentCO2;
                }

                state.States.Add("CO2ppm", _previousCO2);
            }

            if (telemetry.ContainsKey("TemperatureC") && config.HasTemperatureSensor)
            {
                if (double.TryParse(telemetry["TemperatureC"], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var currentTemperatureC))
                    currentTemperatureC = _currentTempAverageValues.GetAverageValue(currentTemperatureC);

                var roundedCurrentTemperatureC = Math.Round(currentTemperatureC, 1);

                if (!_previousTemperatureC.HasValue)
                    _previousTemperatureC = roundedCurrentTemperatureC;
                else
                {
                    if (Math.Abs(roundedCurrentTemperatureC - _previousTemperatureC.Value) > 0.1001)
                        _previousTemperatureC = roundedCurrentTemperatureC;
                }

                state.States.Add("TemperatureC", _previousTemperatureC);
            }

            if (telemetry.ContainsKey("PressureHPa") && config.HasPressureSensor)
            {
                if (double.TryParse(telemetry["PressureHPa"], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var currentPressureHPa))
                    currentPressureHPa = _currentPressureHPaAverageValues.GetAverageValue(currentPressureHPa);

                state.States.Add("PressureHPa", Convert.ToInt32(Math.Round(currentPressureHPa, 0)));
            }

            if (telemetry.ContainsKey("HumidityPercent") && config.HasHumiditySensor)
            {
                if (double.TryParse(telemetry["HumidityPercent"], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var currentHumidityPercent))
                    currentHumidityPercent = _currentHumidityAverageValues.GetAverageValue(currentHumidityPercent);

                var roundedCurrentHumidityPercent = (int)Math.Round(currentHumidityPercent, 0);

                if (!_previousHumidityPercent.HasValue)
                    _previousHumidityPercent = roundedCurrentHumidityPercent;
                else
                {
                    if (Math.Abs(roundedCurrentHumidityPercent - _previousHumidityPercent.Value) > 1)
                        _previousHumidityPercent = roundedCurrentHumidityPercent;
                }

                state.States.Add("HumidityPercent", _previousHumidityPercent);
            }

            if (config.HasPins)
                ParsePinsStates(telemetry, state);

            return state;
        }

        private void ParsePinsStates(Dictionary<string, string> telemetry, ItemState state)
        {
            var pinKeys = telemetry.Where(t => t.Key.StartsWith("pin"));

            foreach (var pinKeyValuePair in pinKeys)
            {
                var pin = pinKeyValuePair.Value == "1";

                state.States.Add(pinKeyValuePair.Key, pin);
            }
        }

        private async Task<string> PostContent(StringContent content, string action = "get",
            string requestParams = null, int maxTries = 1)
        {
            string responseString = null;

            try
            {
                responseString = await AsyncHelpers.RetryOnFault(
                    () => PostContentWithLock(content, action, requestParams), maxTries, () => Task.Delay(2000));
            }
            catch (Exception e)
            {
                // ignored
            }

            return responseString;
        }

        private async Task<string> PostContentWithLock(StringContent content, string action = "get",
            string requestParams = null)
        {
            var config = (Mega2560ControllerConfig)Config;
            string responseString;
            string paramsString = string.IsNullOrWhiteSpace(requestParams) ? string.Empty : $"?{requestParams}";

            try
            {
                await _semaphoreSlimHttpPost.WaitAsync();

                using (var response =
                    await _client.PostAsync($"http://{config.IpAddress}/{action}{paramsString}", content))
                {
                    responseString = await response.Content.ReadAsStringAsync();
                }
            }
            finally
            {
                _semaphoreSlimHttpPost.Release();
            }

            return responseString;
        }

        public override async Task<ISetValueResult> SetValue(string parameter, string value)
        {
            var result = new SetValueResult();

            if (!parameter.StartsWith("pin") || !_availablePinCommands.Contains(value))
            {
                result.Success = false;
                return result;
            }

            var content = new StringContent("", Encoding.UTF8, "application/text");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/text");

            string responseString = await PostContent(content, "set", $"{parameter}={value}");

            if (string.IsNullOrWhiteSpace(responseString))
            {
                result.Success = false;
                return result;
            }

            var state = ParseState(responseString);

            if (state.States.Any())
                state.ConnectionStatus = ConnectionStatus.Stable;
            else
            {
                result.Success = false;
            }

            SetStateSafely(state);

            return result;
        }

        public TransformationResult Transform(string parameter, string value)
        {
            var result = new TransformationResult();

            if (parameter.StartsWith("pin"))
            {
                if (CurrentState.States.ContainsKey(parameter))
                {
                    var currentPinValue = CurrentState.States[parameter];

                    if (bool.TryParse(currentPinValue.ToString(), out bool res))
                    {
                        if (value == "pimp")
                        {
                            result.TransformedValue = !res;
                            result.Status = TransformationStatus.Success;
                            return result;
                        }
                    }
                }
            }

            result.Status = TransformationStatus.Continue;

            return result;
        }
    }
}