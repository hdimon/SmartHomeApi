using System;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiLogger : IDisposable
    {
        void Prompt(string message);
        void Info(string message);
        void Error(Exception ex);
        void Error(string message);
        void Error(Exception exception, string message);
        void Warning(string message);
        void Debug(string message);
    }
}