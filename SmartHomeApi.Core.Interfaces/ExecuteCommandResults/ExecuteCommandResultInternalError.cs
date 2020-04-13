using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces.ExecuteCommandResults
{
    public class ExecuteCommandResultInternalError : ExecuteCommandResultAbstract
    {
        public ExecuteCommandResultInternalError(IList<string> error = null)
        {
            Error = error;
        }

        public IList<string> Error { get; set; }
    }
}