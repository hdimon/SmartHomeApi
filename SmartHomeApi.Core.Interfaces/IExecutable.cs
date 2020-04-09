using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IExecutable
    {
        string ItemId { get; }
        string ItemType { get; }
        Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command);
    }
}