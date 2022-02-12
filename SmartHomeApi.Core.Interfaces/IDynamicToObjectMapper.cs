using System;

namespace SmartHomeApi.Core.Interfaces;

public interface IDynamicToObjectMapper
{
    object Map(object source, Type toType);
}