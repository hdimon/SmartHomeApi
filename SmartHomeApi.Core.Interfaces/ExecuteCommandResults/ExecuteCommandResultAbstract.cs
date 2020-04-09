using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces.ExecuteCommandResults
{
    public abstract class ExecuteCommandResultAbstract
    {
        public bool Success { get; set; }
        public IList<string> Error { get; set; }
    }
}