using SmartHomeApi.Core.Interfaces;

namespace Mega2560ControllerDevice
{
    public class Mega2560ControllerConfig : ItemConfigAbstract
    {
        public string Mac { get; set; }
        public string IpAddress { get; set; }
        public bool HasCO2Sensor { get; set; }
        public bool HasTemperatureSensor { get; set; }
        public bool HasHumiditySensor { get; set; }
        public bool HasPressureSensor { get; set; }
        public bool HasPins { get; set; }
        public bool HasSlave1CO2Sensor { get; set; }
        public bool HasSlave1TemperatureSensor { get; set; }
        public bool HasSlave1HumiditySensor { get; set; }
        public bool HasSlave1PressureSensor { get; set; }

        public Mega2560ControllerConfig(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}