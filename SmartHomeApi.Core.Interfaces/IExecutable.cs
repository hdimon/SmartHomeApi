using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IExecutable : IItem
    {
        Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command);
    }
}