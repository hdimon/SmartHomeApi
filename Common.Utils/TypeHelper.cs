using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace Common.Utils
{
    public class TypeHelper
    {
        public static bool IsSimpleType(Type type)
        {
            return
                type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type.IsEnum ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                 IsSimpleType(type.GetGenericArguments()[0]));
        }

        public static bool IsDictionary(object obj)
        {
            if (obj == null)
                return false;

            return obj is IDictionary &&
                   obj.GetType().IsGenericType &&
                   obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        public static T GetValue<T>(object val)
        {
            if (IsSimpleType(typeof(T)))
            {
                if (typeof(T) == typeof(int))
                    return (T)Convert.ChangeType(val, typeof(int));

                if (typeof(T) == typeof(int?))
                {
                    if (val == null)
                        return default;

                    return (T)Convert.ChangeType(val, typeof(int));
                }

                return (T)val;
            }

            //Try to get it as string
            string v = val as string;

            if (v == null)
                return default;

            return JsonSerializer.Deserialize<T>(v);
        }

        public static T GetValue<T>(object val, T defaultValue)
        {
            if (val == null)
                return defaultValue;

            return GetValue<T>(val);
        }
    }
}