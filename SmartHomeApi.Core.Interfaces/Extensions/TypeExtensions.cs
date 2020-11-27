using System;
using System.Globalization;

namespace SmartHomeApi.Core.Interfaces.Extensions
{
    public static class TypeExtensions
    {
        public static object GetAsObject(this string value, ValueDataType type, CultureInfo culture)
        {
            if (value == null)
                return null;

            switch (type)
            {
                case ValueDataType.String:
                    return value;
                case ValueDataType.Integer:
                    return int.Parse(value, NumberStyles.Any, culture);
                case ValueDataType.Double:
                    return double.Parse(value, NumberStyles.Any, culture);
                case ValueDataType.Decimal:
                    return decimal.Parse(value, NumberStyles.Any, culture);
                case ValueDataType.Boolean:
                    if (value == "1") return true;
                    if (value == "0") return false;
                    return Convert.ToBoolean(value, culture);
                case ValueDataType.DateTime:
                    return DateTime.Parse(value, culture);
                case ValueDataType.TimeSpan:
                    return TimeSpan.Parse(value, culture);
            }

            throw new ArgumentException($"Provided data type [{type}] is not supported.");
        }
    }
}