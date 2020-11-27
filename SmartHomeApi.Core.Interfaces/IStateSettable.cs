using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateSettable
    {
        string ItemId { get; }
        string ItemType { get; }
        Task<ISetValueResult> SetValue(string parameter, object value);
    }
}