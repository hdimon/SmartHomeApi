namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateTransformable
    {
        string ItemId { get; }
        string ItemType { get; }

        TransformationResult Transform(string parameter, string oldValue, string newValue);
    }
}