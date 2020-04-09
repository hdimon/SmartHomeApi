using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces.ExecuteCommandResults
{
    public class ExecuteCommandResultInternalError : ExecuteCommandResultAbstract
    {
        public IList<string> Error { get; set; }
    }
}