using System;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.ItemUtils
{
    public class DateTimeOffsetProvider : IDateTimeOffsetProvider
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}