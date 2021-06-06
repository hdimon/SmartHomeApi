using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateSettable : IItem
    {
        Task<ISetValueResult> SetValue(string parameter, object value);
    }
}