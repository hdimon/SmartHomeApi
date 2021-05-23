using System;
using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ApiStubLogger : IApiLogger
    {
        public List<string> Logs = new List<string>();

        public void Prompt(string message)
        {
            Logs.Add(message);
        }

        public void Info(string message)
        {
            Logs.Add(message);
        }

        public void Error(Exception ex)
        {
            Logs.Add(ex.Message);
        }

        public void Error(string message)
        {
            Logs.Add(message);
        }

        public void Error(Exception exception, string message)
        {
            Logs.Add(message);
        }

        public void Warning(string message)
        {
            Logs.Add(message);
        }

        public void Debug(string message)
        {
            Logs.Add(message);
        }

        public void Dispose()
        {
        }
    }
}