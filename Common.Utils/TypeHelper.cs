using System;
using System.Collections;
using System.Collections.Generic;

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
    }
}