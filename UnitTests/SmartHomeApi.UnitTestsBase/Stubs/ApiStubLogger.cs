using System;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class ApiStubLogger : IApiLogger
    {
        public void Prompt(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Error(Exception ex)
        {
        }

        public void Error(string message)
        {
        }

        public void Error(Exception exception, string message)
        {
        }

        public void Warning(string message)
        {
        }

        public void Debug(string message)
        {
        }
    }
}