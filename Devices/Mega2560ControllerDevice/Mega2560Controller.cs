using SmartHomeApi.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class Mega2560Controller : AutoRefreshDeviceAbstract
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly SemaphoreSlim _semaphoreSlimHttpPost = new SemaphoreSlim(1, 1);
        private readonly AverageValuesHelper _currentTempAverageValues = new AverageValuesHelper(10);
        private readonly AverageValuesHelper _currentCO2AverageValues = new AverageValuesHelper(40);
        private readonly AverageValuesHelper _currentHumidityAverageValues = new AverageValuesHelper(10);
        private readonly AverageValuesHelper _currentPressureHPaAverageValues = new AverageValuesHelper(10);

        public Mega2560Controller(IDeviceHelpersFabric helpersFabric, IItemConfig config) : base(helpersFabric,
            config)
        {
            RefreshIntervalMS = 300;
        }

        protected override async Task<IItemState> RequestData()
        {
            var state = new ItemState(ItemId, ItemType);

            var content = new StringContent("", Encoding.UTF8, "application/text");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/text");

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
                Logger.Error(e);
            }

            if (telemetry == null)
                return state;

            if (telemetry.ContainsKey("Mac"))
            {
                state.States.Add("Mac", telemetry["Mac"]);
            }

            if (telemetry.ContainsKey("CO2ppm"))
            {
                if (double.TryParse(telemetry["CO2ppm"], out var currentCO2))
                    currentCO2 = _currentCO2AverageValues.GetAverageValue(currentCO2);

                state.States.Add("CO2ppm", Convert.ToInt32(Math.Round(currentCO2, 0)));
            }

            if (telemetry.ContainsKey("TemperatureC"))
            {
                if (double.TryParse(telemetry["TemperatureC"], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var currentTemperatureC))
                    currentTemperatureC = _currentTempAverageValues.GetAverageValue(currentTemperatureC);

                state.States.Add("TemperatureC", Math.Round(currentTemperatureC, 1));
            }

            if (telemetry.ContainsKey("PressureHPa"))
            {
                if (double.TryParse(telemetry["PressureHPa"], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var currentPressureHPa))
                    currentPressureHPa = _currentPressureHPaAverageValues.GetAverageValue(currentPressureHPa);

                state.States.Add("PressureHPa", Convert.ToInt32(Math.Round(currentPressureHPa, 0)));
            }

            if (telemetry.ContainsKey("HumidityPercent"))
            {
                if (double.TryParse(telemetry["HumidityPercent"], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var currentHumidityPercent))
                    currentHumidityPercent = _currentHumidityAverageValues.GetAverageValue(currentHumidityPercent);

                state.States.Add("HumidityPercent", Convert.ToInt32(Math.Round(currentHumidityPercent, 0)));
            }

            if (telemetry.ContainsKey("pin0"))
            {
                var pin0 = telemetry["pin0"] == "1";

                state.States.Add("pin0", pin0);
            }

            if (telemetry.ContainsKey("pin1"))
            {
                var pin1 = telemetry["pin1"] == "1";

                state.States.Add("pin1", pin1);
            }

            if (telemetry.ContainsKey("pin2"))
            {
                var pin2 = telemetry["pin2"] == "1";

                state.States.Add("pin2", pin2);
            }

            if (telemetry.ContainsKey("pin3"))
            {
                var pin3 = telemetry["pin3"] == "1";

                state.States.Add("pin3", pin3);
            }

            return state;
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
            var config = (Mega2560ControllerConfig)Config;
            string responseString;

            try
            {
                await _semaphoreSlimHttpPost.WaitAsync();

                var response = await _client.PostAsync($"http://{config.IpAddress}/get", content);
                responseString = await response.Content.ReadAsStringAsync();
            }
            finally
            {
                _semaphoreSlimHttpPost.Release();
            }

            return responseString;
        }

        public override async Task<ISetValueResult> SetValue(string parameter, string value)
        {
            throw new NotImplementedException();
        }
    }
}