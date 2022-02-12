using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class ObjectToDynamicConverter : IObjectToDynamicConverter
    {
        private static readonly BindingFlags PropertiesBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        private static readonly Type IListType = typeof(IList<>);
        private static readonly Type ListType = typeof(List<>);
        private static readonly Type IDictType = typeof(IDictionary<,>);

        public dynamic Convert(object source)
        {
            if (source == null) return null;

            var sourceType = source.GetType();

            if (sourceType.IsValueType || sourceType == typeof(string)) return source;

            IDictionary<string, object> expando = new ExpandoObject();

            if (TypeImplementsGenericInterface(sourceType, IListType))
            {
                var sourceList = (IList)source;

                var genericType = sourceType.GetGenericArguments().First();

                Type listType;

                if (genericType.IsValueType || genericType == typeof(string))
                    listType = ListType.MakeGenericType(genericType);
                else if (genericType == typeof(object))
                    listType = ListType.MakeGenericType(typeof(object));
                else
                    listType = ListType.MakeGenericType(typeof(ExpandoObject));

                var destList = (IList)Activator.CreateInstance(listType, sourceList.Count);

                if (sourceList.Count == 0) return destList;

                foreach (var item in sourceList)
                {
                    //We have to check it every iteration to process case when List<object>
                    if (IsSimpleType(genericType, item))
                    {
                        destList.Add(item);
                        continue;
                    }

                    var childExpando = Convert(item);
                    destList.Add(childExpando);
                }

                return destList;
            }

            if (TypeImplementsGenericInterface(sourceType, IDictType))
            {
                var genericTypes = sourceType.GetGenericArguments();

                if (!genericTypes[0].IsValueType && genericTypes[0] != typeof(string))
                    throw new ArgumentException("Dictionary key must be primitive type");

                var dictSource = (IDictionary)source;

                foreach (DictionaryEntry entry in dictSource)
                {
                    var key = entry.Key.ToString()!;
                    var value = entry.Value;

                    if (value == null)
                    {
                        expando.Add(key, null);
                        continue;
                    }

                    if (IsSimpleType(value.GetType(), value))
                    {
                        expando.Add(key, value);
                        continue;
                    }

                    var childExpando = Convert(value);
                    expando.Add(key, childExpando);
                }

                return expando;
            }

            var sourceProperties = source.GetType().GetProperties(PropertiesBindingFlags);

            foreach (var propertyInfo in sourceProperties)
            {
                var sourcePropertyValue = propertyInfo.GetValue(source);
                var sourcePropertyType = propertyInfo.PropertyType;

                if (IsSimpleType(sourcePropertyType, sourcePropertyType))
                {
                    expando.Add(propertyInfo.Name, sourcePropertyValue);
                    continue;
                }

                var childExpando = Convert(sourcePropertyValue);
                expando.Add(propertyInfo.Name, childExpando);
            }

            return (ExpandoObject)expando;
        }

        private static bool TypeImplementsGenericInterface(Type type, Type interfaceType)
        {
            return type.IsGenericType && interfaceType.IsAssignableFrom(type.GetGenericTypeDefinition()) || type.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType);
        }

        private static bool IsSimpleType(Type type, object sourceObject)
        {
            return type.IsValueType || type == typeof(string) ||
                   type == typeof(object) && (sourceObject.GetType().IsValueType || sourceObject is string);
        }
    }
}