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

        private readonly AverageValuesHelper _currentSlave1TempAverageValues = new AverageValuesHelper(10);
        private readonly AverageValuesHelper _currentSlave1CO2AverageValues = new AverageValuesHelper(40);
        private readonly AverageValuesHelper _currentSlave1HumidityAverageValues = new AverageValuesHelper(10);
        private readonly AverageValuesHelper _currentSlave1PressureHPaAverageValues = new AverageValuesHelper(1000);
        private int? _previousSlave1CO2;
        private int? _previousSlave1HumidityPercent;
        private double? _previousSlave1TemperatureC;

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

            ParseCO2State(telemetry, state);
            ParseTemperatureCState(telemetry, state);
            ParsePressureHPaState(telemetry, state);
            ParseHumidityPercentState(telemetry, state);
            ParsePinsStates(telemetry, state);

            ParseSlave1CO2State(telemetry, state);
            ParseSlave1TemperatureCState(telemetry, state);
            ParseSlave1PressureHPaState(telemetry, state);
            ParseSlave1HumidityPercentState(telemetry, state);

            return state;
        }

        private void ParseCO2State(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("CO2ppm") || !config.HasCO2Sensor)
                return;

            if (double.TryParse(telemetry["CO2ppm"], out var currentCO2))
            {
                if (currentCO2 < 20000)
                    currentCO2 = _currentCO2AverageValues.GetAverageValue(currentCO2);
                else
                    currentCO2 = _currentCO2AverageValues.GetAverageValue(); //Emergency case
            }

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

        private void ParseTemperatureCState(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("TemperatureC") || !config.HasTemperatureSensor)
                return;

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

        private void ParsePressureHPaState(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("PressureHPa") || !config.HasPressureSensor)
                return;

            if (double.TryParse(telemetry["PressureHPa"], NumberStyles.Any, CultureInfo.InvariantCulture,
                out var currentPressureHPa))
                currentPressureHPa = _currentPressureHPaAverageValues.GetAverageValue(currentPressureHPa);

            state.States.Add("PressureHPa", Convert.ToInt32(Math.Round(currentPressureHPa, 0)));
        }

        private void ParseHumidityPercentState(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("HumidityPercent") || !config.HasHumiditySensor)
                return;

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

        private void ParseSlave1CO2State(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("Slave1CO2ppm") || !config.HasSlave1CO2Sensor)
                return;

            if (double.TryParse(telemetry["Slave1CO2ppm"], out var currentCO2))
            {
                if (currentCO2 < 20000)
                    currentCO2 = _currentSlave1CO2AverageValues.GetAverageValue(currentCO2);
                else
                    currentCO2 = _currentSlave1CO2AverageValues.GetAverageValue(); //Emergency case
            }

            var roundedCurrentCO2 = (int)Math.Round(currentCO2, 0);

            if (!_previousSlave1CO2.HasValue)
                _previousSlave1CO2 = roundedCurrentCO2;
            else
            {
                if (Math.Abs(roundedCurrentCO2 - _previousSlave1CO2.Value) > 2)
                    _previousSlave1CO2 = roundedCurrentCO2;
            }

            state.States.Add("Slave1CO2ppm", _previousSlave1CO2);
        }

        private void ParseSlave1TemperatureCState(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("Slave1TemperatureC") || !config.HasSlave1TemperatureSensor)
                return;

            if (double.TryParse(telemetry["Slave1TemperatureC"], NumberStyles.Any, CultureInfo.InvariantCulture,
                out var currentTemperatureC))
                currentTemperatureC = _currentSlave1TempAverageValues.GetAverageValue(currentTemperatureC);

            var roundedCurrentTemperatureC = Math.Round(currentTemperatureC, 1);

            if (!_previousSlave1TemperatureC.HasValue)
                _previousSlave1TemperatureC = roundedCurrentTemperatureC;
            else
            {
                if (Math.Abs(roundedCurrentTemperatureC - _previousSlave1TemperatureC.Value) > 0.1001)
                    _previousSlave1TemperatureC = roundedCurrentTemperatureC;
            }

            state.States.Add("Slave1TemperatureC", _previousSlave1TemperatureC);
        }

        private void ParseSlave1PressureHPaState(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("Slave1PressureHPa") || !config.HasSlave1PressureSensor)
                return;

            if (double.TryParse(telemetry["Slave1PressureHPa"], NumberStyles.Any, CultureInfo.InvariantCulture,
                out var currentPressureHPa))
                currentPressureHPa = _currentSlave1PressureHPaAverageValues.GetAverageValue(currentPressureHPa);

            state.States.Add("Slave1PressureHPa", Convert.ToInt32(Math.Round(currentPressureHPa, 0)));
        }

        private void ParseSlave1HumidityPercentState(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!telemetry.ContainsKey("Slave1HumidityPercent") || !config.HasSlave1HumiditySensor)
                return;

            if (double.TryParse(telemetry["Slave1HumidityPercent"], NumberStyles.Any, CultureInfo.InvariantCulture,
                out var currentHumidityPercent))
                currentHumidityPercent = _currentSlave1HumidityAverageValues.GetAverageValue(currentHumidityPercent);

            var roundedCurrentHumidityPercent = (int)Math.Round(currentHumidityPercent, 0);

            if (!_previousSlave1HumidityPercent.HasValue)
                _previousSlave1HumidityPercent = roundedCurrentHumidityPercent;
            else
            {
                if (Math.Abs(roundedCurrentHumidityPercent - _previousSlave1HumidityPercent.Value) > 1)
                    _previousSlave1HumidityPercent = roundedCurrentHumidityPercent;
            }

            state.States.Add("Slave1HumidityPercent", _previousSlave1HumidityPercent);
        }

        private void ParsePinsStates(Dictionary<string, string> telemetry, ItemState state)
        {
            var config = (Mega2560ControllerConfig)Config;

            if (!config.HasPins)
                return;

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

        public TransformationResult Transform(string parameter, string oldValue, string value)
        {
            var result = new TransformationResult();

            if (parameter.StartsWith("pin"))
            {
                if (bool.TryParse(oldValue, out bool res))
                {
                    if (value == "pimp")
                    {
                        result.TransformedValue = !res;
                        result.Status = TransformationStatus.Success;
                        return result;
                    }
                }

                if (value == "high")
                {
                    result.TransformedValue = true;
                    result.Status = TransformationStatus.Success;
                    return result;
                }

                if (value == "low")
                {
                    result.TransformedValue = false;
                    result.Status = TransformationStatus.Success;
                    return result;
                }
            }

            result.Status = TransformationStatus.Continue;

            return result;
        }
    }
}