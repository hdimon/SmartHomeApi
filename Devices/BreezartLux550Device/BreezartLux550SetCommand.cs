using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace BreezartLux550Device
{
    class BreezartLux550SetCommand
    {
        public string Parameter { get; set; }
        public string Value { get; set; }

        public TaskCompletionSource<ISetValueResult> TaskCompletionSource { get; }

        public BreezartLux550SetCommand(string parameter, string value)
        {
            Parameter = parameter;
            Value = value;
            TaskCompletionSource = new TaskCompletionSource<ISetValueResult>();
        }
    }
}