using System;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDateTimeOffsetProvider
    {
        DateTimeOffset Now { get; }
    }
}