namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateTransformable
    {
        string ItemType { get; }

        TransformationResult Transform(string parameter, string value);
    }
}