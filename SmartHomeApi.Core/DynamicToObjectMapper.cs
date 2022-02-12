using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    //Implementation of this class is far from perfect but it seems to do what it should and covered with unit-tests
    //so I don't want to spend time on improvements right now.
    public class DynamicToObjectMapper : IDynamicToObjectMapper
    {
        private static readonly BindingFlags PropertiesBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        private static readonly Type IListType = typeof(IList<>);
        private static readonly Type ListType = typeof(List<>);
        private static readonly Type IDictType = typeof(IDictionary<,>);
        private static readonly Type DictType = typeof(Dictionary<,>);

        public object Map(object source, Type destinationType)
        {
            if (source == null) return null;

            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            if (destinationType.IsValueType || destinationType == typeof(string))
                return GetDestinationValue(source, destinationType);

            var destinationObject = Activator.CreateInstance(destinationType);

            if (TypeImplementsGenericInterface(source.GetType(), IListType))
            {
                MapToRootList(source, destinationObject, destinationType);
                return destinationObject;
            }

            if (source is ExpandoObject sourceExpando)
            {
                Map(sourceExpando, destinationObject);
                return destinationObject;
            }

            if (TypeImplementsGenericInterface(source.GetType(), IDictType))
            {
                MapToRootDictionary(source, destinationObject);
                return destinationObject;
            }

            return destinationObject;
        }

        private void MapToRootList(object source, object destinationObject, Type destinationType)
        {
            var genericType = source.GetType().GetGenericArguments().First();
            var sourceList = (IList)source;
            var destList = (IList)destinationObject;
            var destGenericType = destinationType.GetGenericArguments().First();

            if (sourceList.Count == 0) return;

            foreach (var item in sourceList)
            {
                //We have to check it every iteration to process case when List<object>
                if (IsSimpleType(genericType, item))
                {
                    destList.Add(item);
                    continue;
                }

                var childObject = Map(item, destGenericType);

                destList.Add(childObject);
            }
        }

        private static void MapToRootDictionary(object source, object destinationObject)
        {
            //It's enough if source object is at least IEnumerable
            var sourceDict = (IEnumerable)source;
            var genericTypes = destinationObject.GetType().GetGenericArguments();

            foreach (var sourceItem in sourceDict)
            {
                AddDictionaryValue(sourceItem, (IDictionary)destinationObject, genericTypes);
            }
        }

        private static void Map(ExpandoObject sourceObject, object destObject)
        {
            //In case if root of dynamic object is converted to Dictionary<K,V>
            if (TypeImplementsGenericInterface(destObject.GetType(), IDictType))
            {
                MapToDictionary(sourceObject, destObject);

                return;
            }

            var destObjectPropsMap = destObject.GetType().GetProperties(PropertiesBindingFlags)
                                               .ToDictionary(p => p.Name.ToLower(), p => p);

            foreach (var sourcePropPair in sourceObject)
            {
                if (!destObjectPropsMap.TryGetValue(sourcePropPair.Key.ToLower(), out var destPropInfo))
                    continue;

                MapProperty(sourcePropPair.Value, destObject, destPropInfo);
            }
        }

        private static void MapToDictionary(ExpandoObject sourceObject, object destObject)
        {
            //It's enough if source object is at least IEnumerable
            var sourceDict = (IEnumerable)sourceObject;
            var genericTypes = destObject.GetType().GetGenericArguments();

            foreach (var sourceItem in sourceDict)
            {
                AddDictionaryValue(sourceItem, (IDictionary)destObject, genericTypes);
            }
        }

        private static void MapProperty(object sourceObject, object destObject, PropertyInfo destPropInfo)
        {
            if (sourceObject == null) return;

            Type destPropType = destPropInfo.PropertyType;

            if (IsSimpleType(destPropType, sourceObject))
            {
                SetValue(sourceObject, destObject, destPropInfo);
            }
            else if (TypeImplementsGenericInterface(destPropType, IListType))
            {
                CreateList(sourceObject, destObject, destPropInfo);
            }
            else if (TypeImplementsGenericInterface(destPropType, IDictType))
            {
                CreateDictionary(sourceObject, destObject, destPropInfo);
            }
            else
            {
                var nestedObject = Activator.CreateInstance(destPropType);

                Map((ExpandoObject)sourceObject, nestedObject);

                destPropInfo.SetValue(destObject, nestedObject, null);
            }
        }

        private static bool IsSimpleType(Type type, object sourceObject)
        {
            return type.IsValueType || type == typeof(string) ||
                   type == typeof(object) && (sourceObject.GetType().IsValueType || sourceObject is string);
        }

        private static bool TypeImplementsGenericInterface(Type type, Type interfaceType)
        {
            return type.IsGenericType && interfaceType.IsAssignableFrom(type.GetGenericTypeDefinition()) || type.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType);
        }

        private static void SetValue(object sourceObject, object destObject, PropertyInfo destPropInfo)
        {
            Type destPropType = destPropInfo.PropertyType;

            var destValue = GetDestinationValue(sourceObject, destPropType);

            destPropInfo.SetValue(destObject, destValue, null);
        }

        private static void CreateList(object sourceObject, object destObject, PropertyInfo destPropInfo)
        {
            Type destPropType = destPropInfo.PropertyType;

            //It's enough if source object is at least ICollection
            var sourceList = (IList)sourceObject;

            var genericType = destPropType.GetGenericArguments().First();
            var listType = ListType.MakeGenericType(genericType);
            var list = (IList)Activator.CreateInstance(listType, sourceList.Count);

            foreach (var sourceItem in sourceList)
            {
                AddListValue(sourceItem, list, genericType);
            }

            destPropInfo.SetValue(destObject, list, null);
        }

        private static void CreateDictionary(object sourceObject, object destObject, PropertyInfo destPropInfo)
        {
            Type destPropType = destPropInfo.PropertyType;

            //It's enough if source object is at least IEnumerable
            var sourceDict = (IEnumerable)sourceObject;

            var genericTypes = destPropType.GetGenericArguments();
            var dictType = DictType.MakeGenericType(genericTypes);
            var dict = (IDictionary)Activator.CreateInstance(dictType);

            foreach (var sourceItem in sourceDict)
            {
                AddDictionaryValue(sourceItem, dict, genericTypes);
            }

            destPropInfo.SetValue(destObject, dict, null);
        }

        private static void AddListValue(object sourceObject, IList destList, Type genericParamType)
        {
            if (IsSimpleType(genericParamType, sourceObject))
            {
                var destValue = GetDestinationValue(sourceObject, genericParamType);

                destList.Add(destValue);
                return;
            }

            var destinationObject = Activator.CreateInstance(genericParamType);
            Map((ExpandoObject)sourceObject, destinationObject);

            destList.Add(destinationObject);
        }

        private static void AddDictionaryValue(object sourceObject, IDictionary destDict, Type[] genericParamTypes)
        {
            var valueType = genericParamTypes[1];

            Type sourceObjectType = sourceObject.GetType();

            var sourceKey = sourceObjectType.GetProperty("Key")?.GetValue(sourceObject);

            if (sourceKey == null)
                throw new ArgumentException("Error occurred during dictionary mapping");

            var sourceValue = sourceObjectType.GetProperty("Value")?.GetValue(sourceObject);

            if (sourceValue == null)
                throw new ArgumentException("Error occurred during dictionary mapping");

            if (IsSimpleType(valueType, sourceObject))
            {
                var destValue = GetDestinationValue(sourceValue, valueType);

                destDict.Add(sourceKey, destValue);
                return;
            }

            var destinationObject = Activator.CreateInstance(valueType);

            if (TypeImplementsGenericInterface(sourceValue.GetType(), IListType))
            {
                var sourceList = (IList)sourceValue;
                var destGenericType = valueType.GetGenericArguments().First();
                var destList = (IList)destinationObject;

                foreach (var item in sourceList)
                {
                    AddListValue(item, destList, destGenericType);
                }
            }
            else
            {
                Map((ExpandoObject)sourceValue, destinationObject);
            }

            destDict.Add(sourceKey, destinationObject);
        }

        private static object GetDestinationValue(object sourceObject, Type destObjectType)
        {
            if (sourceObject is long && destObjectType == typeof(int))
                return Convert.ChangeType(sourceObject, TypeCode.Int32);

            return sourceObject;
        }
    }
}