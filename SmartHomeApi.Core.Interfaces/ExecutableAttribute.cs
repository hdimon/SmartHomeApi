using System;

namespace SmartHomeApi.Core.Interfaces
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExecutableAttribute : Attribute
    {
        public ExecutableAttribute()
        {
        }
    }
}