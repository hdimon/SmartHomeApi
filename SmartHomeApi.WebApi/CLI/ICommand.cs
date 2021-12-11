using System.Threading.Tasks;

namespace SmartHomeApi.WebApi.CLI
{
    public interface ICommand
    {
        Task<int> Execute(CLIContext cliCtx);
    }
}