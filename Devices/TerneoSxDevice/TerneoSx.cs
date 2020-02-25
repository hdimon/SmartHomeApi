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
    public class TerneoSx : AutoRefreshDeviceAbstract
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly SemaphoreSlim _semaphoreSlimHttpPost = new SemaphoreSlim(1, 1);
        private readonly AverageValuesHelper _currentTempAverageValuesHelper = new AverageValuesHelper(10);
        private double? _previousCurrentTemp;

        private readonly string CurrentTemperatureParameter = "CurrentTemperature";
        private readonly string SetTemperatureParameter = "SetTemperature";
        private readonly string HeatingParameter = "Heating";
        private readonly string PowerParameter = "Power";

        private readonly List<string> _settableParametersList;

        public TerneoSx(IItemHelpersFabric helpersFabric, IItemConfig config) : base(helpersFabric, config)
        {
            _settableParametersList = new List<string> { SetTemperatureParameter };
        }

        protected override Task InitializeDevice()
        {
            _client.Timeout = new TimeSpan(0, 0, 10);

            return base.InitializeDevice();
        }

        public override async Task<ISetValueResult> SetValue(string parameter, string value)
        {
            var result = new SetValueResult(ItemId, ItemType);

            if (!_settableParametersList.Contains(parameter))
            {
                result.Success = false;
                return result;
            }

            var config = (TerneoSxConfig)Config;

            var content = new StringContent($"{{\"sn\":\"{config.SerialNumber}\", \"par\":[[5,1,\"{value}\"]]}}", Encoding.UTF8,
                "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            string responseString = await PostContent(content, 3);

            if (string.IsNullOrWhiteSpace(responseString) || responseString.Trim() != "{\"success\":\"true\"}")
            {
                result.Success = false;
                return result;
            }

            return result;
        }

        private async Task<string> PostContent(StringContent content, int maxTries = 2)
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

            return responseString?.Trim();
        }

        private async Task<string> PostContentWithLock(StringContent content)
        {
            var config = (TerneoSxConfig)Config;
            string responseString;

            try
            {
                await _semaphoreSlimHttpPost.WaitAsync();

                using (var response = await _client.PostAsync($"http://{config.IpAddress}/api.cgi", content))
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

        protected override async Task<IItemState> RequestData()
        {
            var state = new ItemState(ItemId, ItemType);

            var content = new StringContent("{\"cmd\":4}", Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            string responseString = await PostContent(content);

            if (string.IsNullOrWhiteSpace(responseString) || !responseString.StartsWith("{") || !responseString.EndsWith("}"))
                return state;

            Dictionary<string, string> telemetry = null;

            try
            {
                telemetry = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }
            catch (Exception e)
            {
                /*Logger.Error(e);
                Logger.Error($"String failed on deserializing was: {responseString}");*/
                Logger.Error("Response failed on deserializing");
            }

            if (telemetry == null)
                return state;

            if (telemetry.ContainsKey("success") && telemetry["success"] == "false")
                return state;

            if (telemetry.ContainsKey("t.1"))
            {
                if (double.TryParse(telemetry["t.1"], out var currentTemp))
                    currentTemp = _currentTempAverageValuesHelper.GetAverageValue(currentTemp);

                var roundedCurrentTemp = Math.Round(currentTemp / 16, 1);

                if (!_previousCurrentTemp.HasValue)
                    _previousCurrentTemp = roundedCurrentTemp;
                else
                {
                    if (Math.Abs(roundedCurrentTemp - _previousCurrentTemp.Value) > 0.2001)
                        _previousCurrentTemp = roundedCurrentTemp;
                }

                state.States.Add(CurrentTemperatureParameter, _previousCurrentTemp);
            }
            if (telemetry.ContainsKey("t.5"))
            {
                double.TryParse(telemetry["t.5"], out var setTemp);
                state.States.Add(SetTemperatureParameter, Math.Round(setTemp / 16, 1));
            }
            if (telemetry.ContainsKey("f.0"))
            {
                var heating = telemetry["f.0"] == "1";

                state.States.Add(HeatingParameter, heating);
            }

            return state;
        }

        protected override void ExtendItemStates(IItemState state)
        {
            var config = (TerneoSxConfig)Config;

            if (state?.States == null) 
                return;

            if (state.States.ContainsKey(HeatingParameter))
            {
                var heating = (bool)state.States[HeatingParameter];

                var power = heating ? config.Power : 0;

                if (state.States.ContainsKey(PowerParameter))
                    state.States[PowerParameter] = power;
                else
                    state.States.Add(PowerParameter, power);
            }
        }
    }
}