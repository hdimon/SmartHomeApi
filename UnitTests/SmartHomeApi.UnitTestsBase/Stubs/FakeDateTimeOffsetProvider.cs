using System;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class FakeDateTimeOffsetProvider : IDateTimeOffsetProvider
    {
        private DateTimeOffset _now;

        public DateTimeOffset Now => _now;

        public void SetNow(DateTimeOffset date)
        {
            _now = date;
        }
    }
}