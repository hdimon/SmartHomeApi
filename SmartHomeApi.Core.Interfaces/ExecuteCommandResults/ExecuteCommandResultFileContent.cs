namespace SmartHomeApi.Core.Interfaces.ExecuteCommandResults
{
    public class ExecuteCommandResultFileContent : ExecuteCommandResultAbstract
    {
        public ExecuteCommandResultFileContent()
        {
        }

        public ExecuteCommandResultFileContent(byte[] content, string contentType)
        {
            FileContents = content;
            ContentType = contentType;
        }

        public byte[] FileContents { get; set; }
        public string ContentType { get; set; }
    }
}