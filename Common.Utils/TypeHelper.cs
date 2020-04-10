﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

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

        public static dynamic ToDynamic<T>(T obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                var propertyExpression = Expression.Property(Expression.Constant(obj), propertyInfo);
                var currentValue = Expression.Lambda<Func<string>>(propertyExpression).Compile().Invoke();
                expando.Add(propertyInfo.Name.ToLower(), currentValue);
            }

            return expando as ExpandoObject;
        }

        public static dynamic ObjToDynamic(object obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                var propertyExpression = Expression.Property(Expression.Constant(obj), propertyInfo);
                var currentValue = Expression.Lambda<Func<string>>(propertyExpression).Compile().Invoke();
                expando.Add(propertyInfo.Name.ToLower(), currentValue);
            }

            return expando as ExpandoObject;
        }
    }
}