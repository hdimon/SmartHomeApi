namespace SmartHomeApi.Core.Interfaces
{
    public interface IJsonSerializer
    {
        string Serialize(object value);
        T Deserialize<T>(string value);
    }
}