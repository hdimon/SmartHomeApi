namespace SmartHomeApi.Core.Interfaces
{
    public class TransformationResult
    {
        public TransformationStatus Status { get; set; }
        public object TransformedValue { get; set; }
    }
}