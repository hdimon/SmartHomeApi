namespace SmartHomeApi.Core.Interfaces
{
    public class SetValueResult : ISetValueResult
    {
        public bool Success { get; set; }

        public SetValueResult(bool success = true)
        {
            Success = success;
        }
    }
}