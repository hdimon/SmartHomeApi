namespace SmartHomeApi.Core.Interfaces.ExecuteCommandResults
{
    public class ExecuteCommandResultFileContent : ExecuteCommandResultAbstract
    {
        public byte[] FileContents { get; set; }
        public string ContentType { get; set; }
    }
}