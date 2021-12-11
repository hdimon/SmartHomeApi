using System.Collections.Generic;

namespace SmartHomeApi.WebApi.CLI
{
    public class CLIContext
    {
        public List<string> Args { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
    }
}