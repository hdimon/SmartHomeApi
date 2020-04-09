using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public class ExecuteCommand
    {
        public string ItemId { get; set; }
        public string Command { get; set; }
        public ExecuteCommandHttpMethod HttpMethod { get; set; }
        public IDictionary<string, string> QueryParams { get; set; }
        public IDictionary<string, object> BodyParams { get; set; }
    }
}